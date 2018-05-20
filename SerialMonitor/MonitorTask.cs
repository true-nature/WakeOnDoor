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

            using (var writer = new SyslogWriter(Facility.local0, "WakeOnDoor"))
            {
                var opened = await writer.OpenAsync();
                if (opened)
                {
                    twatcher = new TweLiteWatcher();
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
    }
}
