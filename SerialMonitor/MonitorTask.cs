using AppServiceMessage;
using Nito.AsyncEx;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Storage;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace SerialMonitor
{
    public sealed class MonitorTask : IBackgroundTask
    {
        private AppServiceConnection appConn;
        private LogWriter writer;
        private TweLiteWatcher twatcher;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            await initSettingsAsync();
            taskInstance.Canceled += this.OnCanceled;

            writer = new LogWriter();
            var opened = await writer.OpenAsync();
            if (opened)
            {
                twatcher = new TweLiteWatcher(writer);
                await twatcher.WatchAsync();
                twatcher.Dispose();
            }
            writer.Dispose();
            deferral.Complete();
        }

        private async Task initSettingsAsync()
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (!settings.Values.ContainsKey(AppMessage.KEY_MACLIST))
            {
                var macList = new HashSet<string>();
                SaveSetting(settings, AppMessage.KEY_MACLIST, macList);
            }
            var conn = new AppServiceConnection();
            conn.AppServiceName = "TweLiteMonitor";
            conn.PackageFamilyName = "TweLiteMonitor-uwp_mtz6gfc7cpfh4";
            var mre = new AsyncManualResetEvent(false);
            var op = conn.OpenAsync();
            op.Completed += (sender, args) => { mre.Set(); };
            await mre.WaitAsync();
            var status = op.GetResults();
            if (status == AppServiceConnectionStatus.Success)
            {
                conn.RequestReceived += OnRequestReceived;
                appConn = conn;
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

        private static void ReadSetting(ApplicationDataContainer settings, string key, object value)
        {
            var serializer = new DataContractSerializer(value.GetType());
            var str = (string)settings.Values[key];
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(str)))
            {
                value = serializer.ReadObject(stream);
            }
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
#pragma warning disable CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
            writer.WriteAsync(reason.ToString());
#pragma warning restore CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
            twatcher?.Stop();
        }

        private void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var macList = new HashSet<string>(ApplicationData.Current.LocalSettings.Values[AppMessage.KEY_MACLIST] as string[]);

            var values = new ValueSet();
            var message = args.Request.Message;
            object command;
            if (message.TryGetValue(AppMessage.KEY_COMMAND, out command))
            {
                object macaddr;
                switch(command)
                {
                    case AppMessage.CMD_ADD:
                        if (message.TryGetValue(AppMessage.KEY_MAC_ADDRESS, out macaddr))
                        {
                            values[AppMessage.KEY_RESULT] = macList.Add(macaddr as string);
                        }
                        break;
                    case AppMessage.CMD_REMOVE:
                        if (message.TryGetValue(AppMessage.KEY_MAC_ADDRESS, out macaddr))
                        {
                            values[AppMessage.KEY_RESULT] = macList.Remove(macaddr as string);
                        }
                        break;
                    case AppMessage.CMD_GET:
                        break;
                    default:
                        break;
                }
            }
            var list = new string[macList.Count];
            macList.CopyTo(list);
            values[AppMessage.KEY_MACLIST] = list;
            var result = args.Request.SendResponseAsync(values);
        }
    }

    internal class Settings
    {
        public string[] maclist { get; set; }
    }
}
