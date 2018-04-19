using System.Collections.Generic;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Storage;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace SerialMonitor
{
    public sealed class MonitorTask : IBackgroundTask
    {
        private LogWriter writer;
        private TweLiteWatcher twatcher;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            SettingsEditor.InitSettings();
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


        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
#pragma warning disable CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
            writer.WriteAsync(reason.ToString());
#pragma warning restore CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
            twatcher?.Stop();
        }

        private void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var macList = new HashSet<string>(ApplicationData.Current.LocalSettings.Values[nameof(Keys.MacList)] as string[]);

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
                            values[nameof(Keys.Result)] = macList.Add(macaddr as string);
                        }
                        break;
                    case nameof(AppCommands.Remove):
                        if (message.TryGetValue(nameof(Keys.MacAddress), out macaddr))
                        {
                            values[nameof(Keys.Result)] = macList.Remove(macaddr as string);
                        }
                        break;
                    case nameof(AppCommands.Get):
                        break;
                    default:
                        break;
                }
            }
            var list = new string[macList.Count];
            macList.CopyTo(list);
            values[nameof(Keys.MacList)] = list;
            var result = args.Request.SendResponseAsync(values);
        }
    }
}
