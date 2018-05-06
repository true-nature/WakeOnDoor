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
        private TweLiteWatcher twatcher;
        private BackgroundTaskCancellationReason CancelReason;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            SettingsEditor.InitSettings();
            taskInstance.Canceled += this.OnCanceled;

            using (var writer = new LogWriter(Facility.local0, "WakeOnDoor"))
            {
                var opened = await writer.OpenAsync();
                if (opened)
                {
                    twatcher = new TweLiteWatcher(writer);
                    await twatcher.WatchAsync();
                    await writer.Warning(CancelReason.ToString());
                    twatcher.Dispose();
                }
            }
            deferral.Complete();
        }


        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            CancelReason = reason;
            twatcher?.Stop();
        }
#if false
        private void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var macList = new HashSet<string>(ApplicationData.Current.LocalSettings.Values[nameof(Keys.TargetList)] as string[]);

            var values = new ValueSet();
            var message = args.Request.Message;
            if (message.TryGetValue(nameof(Keys.Command), out object command))
            {
                object macaddr;
                switch (command)
                {
                    case nameof(AppCommands.Add):
                        if (message.TryGetValue(nameof(Keys.PhysicalAddress), out macaddr))
                        {
                            values[nameof(Keys.Result)] = macList.Add(macaddr as string);
                        }
                        break;
                    case nameof(AppCommands.Remove):
                        if (message.TryGetValue(nameof(Keys.PhysicalAddress), out macaddr))
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
            values[nameof(Keys.TargetList)] = list;
            var result = args.Request.SendResponseAsync(values);
        }
#endif
    }
}
