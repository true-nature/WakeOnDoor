using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
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
            object value;
            ReadSetting(settings, nameof(Keys.MacList), typeof(HashSet<string>), out value);
            var macList = value as HashSet<string>;

            var values = new ValueSet();
            var message = args.Request.Message;
            object command;
            if (message.TryGetValue(nameof(Keys.Command), out command))
            {
                object macaddr;
                switch (command)
                {
                    case nameof(AppCommands.Add):
                        if (message.TryGetValue(nameof(Keys.MacAddress), out macaddr))
                        {
                            values[nameof(Keys.Result)] = macList.Add(macaddr as string);
                            SaveSetting(settings, nameof(Keys.MacList), macList);
                        }
                        break;
                    case nameof(AppCommands.Remove):
                        if (message.TryGetValue(nameof(Keys.MacAddress), out macaddr))
                        {
                            values[nameof(Keys.Result)] = macList.Remove(macaddr as string);
                            SaveSetting(settings, nameof(Keys.MacList), macList);
                        }
                        break;
                    case nameof(AppCommands.Get):
                        break;
                    default:
                        break;
                }
            }
            values[nameof(Keys.MacList)] = settings.Values[nameof(Keys.MacList)];
            var result = args.Request.SendResponseAsync(values);
        }

        internal static void InitSettings()
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (!settings.Values.ContainsKey(nameof(Keys.MacList)))
            {
                var macList = new HashSet<string>();
                SaveSetting(settings, nameof(Keys.MacList), macList);
            }
        }

        private static void SaveSetting(ApplicationDataContainer settings, string key, object value)
        {
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractSerializer(value.GetType());
                serializer.WriteObject(stream, value);
                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
                    settings.Values.Add(key, reader.ReadToEnd());
                }
            }
        }

        private static void ReadSetting(ApplicationDataContainer settings, string key, Type t, out object value)
        {
            var serializer = new DataContractSerializer(t);
            var str = (string)settings.Values[key];
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(str)))
            {
                value = serializer.ReadObject(stream);
            }
        }
    }
}
