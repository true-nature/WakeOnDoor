using Nito.AsyncEx;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.UI.Core;

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

        void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
#pragma warning disable CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
            writer.WriteAsync(reason.ToString());
#pragma warning restore CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
            twatcher?.Stop();
        }
    }
}
