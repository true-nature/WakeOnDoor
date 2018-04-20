using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace SerialMonitor
{
    public sealed class SettingsEditor : IBackgroundTask
    {
        private BackgroundTaskDeferral taskDeferral;
        private AppServiceConnection connection;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            taskDeferral = taskInstance.GetDeferral();
            taskInstance.Canceled += this.OnCanceled;
            InitSettings();
            var detail = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            connection = detail?.AppServiceConnection;
            if (connection != null)
            {
                connection.RequestReceived += OnRequestReceived;
            }
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            taskDeferral?.Complete();
            taskDeferral = null;
        }

        private void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var settings = ApplicationData.Current.LocalSettings;
            var macList = ReadMacList(settings);

            var values = new ValueSet();
            var message = args.Request.Message;
            if (message.TryGetValue(nameof(Keys.Command), out object command))
            {
                object physical;
                object comment;
                switch (command)
                {
                    case nameof(AppCommands.Add):
                        if (message.TryGetValue(nameof(Keys.MacAddress), out physical)
                            && message.TryGetValue(nameof(Keys.Comment), out comment))
                        {
                            var target = new WOLTarget() { Physical = physical as string, Comment = comment as string };
                            var exist = macList.ContainsKey(target.Physical);
                            macList[target.Physical] = target;  // replace if exist
                            values[nameof(Keys.Result)] = (!exist).ToString();
                            SaveMacList(settings, macList);
                        }
                        break;
                    case nameof(AppCommands.Remove):
                        if (message.TryGetValue(nameof(Keys.MacAddress), out physical)
                            && message.TryGetValue(nameof(Keys.Comment), out comment))
                        {
                            var target = new WOLTarget() { Physical = physical as string, Comment = comment as string };
                            var exist = macList.ContainsKey(target.Physical);
                            if (exist) { macList.Remove(target.Physical); }
                            values[nameof(Keys.Result)] = exist.ToString();
                            SaveMacList(settings, macList);
                        }
                        break;
                    case nameof(AppCommands.Get):
                        values[nameof(Keys.Result)] = true.ToString();
                        break;
                    default:
                        break;
                }
            }
            values[nameof(Keys.TargetList)] = settings.Values[nameof(Keys.TargetList)];
            var result = args.Request.SendResponseAsync(values);
        }

        internal static void InitSettings()
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (!settings.Values.ContainsKey(nameof(Keys.TargetList)))
            {
                using (var stream = new MemoryStream())
                {
                    var targetSet = new HashSet<WOLTarget>();
                    var serializer = new DataContractJsonSerializer(typeof(HashSet<WOLTarget>));
                    serializer.WriteObject(stream, targetSet);
                    var strToSave = Encoding.UTF8.GetString(stream.ToArray());
                    settings.Values[nameof(Keys.TargetList)] = strToSave;
                }
            }
        }

        private static Dictionary<string, WOLTarget> ReadMacList(ApplicationDataContainer settings)
        {
            Dictionary<string, WOLTarget> result = new Dictionary<string, WOLTarget>();
            var targetListStr = (settings.Values[nameof(Keys.TargetList)] as string);
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(targetListStr)))
            {
                var serializer = new DataContractJsonSerializer(typeof(HashSet<WOLTarget>));
                var targets = serializer.ReadObject(stream) as HashSet<WOLTarget>;
                foreach (var t in targets)
                {
                    result.Add(t.Physical, t);
                }
            }
            return result;
        }

        private static string SaveMacList(ApplicationDataContainer settings, Dictionary<string,WOLTarget> targetList)
        {
            using (var stream = new MemoryStream())
            {
                var targetSet = new HashSet<WOLTarget>(targetList.Values);
                var serializer = new DataContractJsonSerializer(typeof(HashSet<WOLTarget>));
                serializer.WriteObject(stream, targetSet);
                var strToSave = Encoding.UTF8.GetString(stream.ToArray());
                settings.Values[nameof(Keys.TargetList)] = strToSave;
                return strToSave;
            }
        }

    }
}
