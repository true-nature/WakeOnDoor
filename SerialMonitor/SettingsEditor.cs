using System.Collections.Generic;
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
        private static char[] separators = { ',', ';' };

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
                object macaddr;
                switch (command)
                {
                    case nameof(AppCommands.Add):
                        if (message.TryGetValue(nameof(Keys.MacAddress), out macaddr))
                        {
                            values[nameof(Keys.Result)] = macList.Add(macaddr as string).ToString();
                            SaveMacList(settings, macList);
                        }
                        break;
                    case nameof(AppCommands.Remove):
                        if (message.TryGetValue(nameof(Keys.MacAddress), out macaddr))
                        {
                            values[nameof(Keys.Result)] = macList.Remove(macaddr as string).ToString();
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
            values[nameof(Keys.MacList)] = settings.Values[nameof(Keys.MacList)];
            var result = args.Request.SendResponseAsync(values);
        }

        internal static void InitSettings()
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (!settings.Values.ContainsKey(nameof(Keys.MacList)))
            {
                settings.Values[nameof(Keys.MacList)] = "";
            }
        }

        private static HashSet<string> ReadMacList(ApplicationDataContainer settings)
        {
            var macArray = (settings.Values[nameof(Keys.MacList)] as string)?.Trim().Split(separators);
            var macList = new HashSet<string>();
            foreach (var m in macList) { macList.Add(m); }
            return macList;
        }

        private static string SaveMacList(ApplicationDataContainer settings, HashSet<string> macList)
        {
            var joinedStr = string.Join(",", macList);
            settings.Values[nameof(Keys.MacList)] = string.Join(",", macList);
            return joinedStr;
        }

    }
}
