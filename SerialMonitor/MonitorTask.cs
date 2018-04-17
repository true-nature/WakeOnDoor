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
        private static DeviceWatcher watcher = null;
        private static bool isEnumerated = false;

        private ISerialCommService commService;
        private bool IsConnected { get; set; }
        private SemaphoreSlim semaphore;
        private LogWriter writer;
        private bool Canceled;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += this.OnCanceled;
            IsConnected = false;
            semaphore = new SemaphoreSlim(1, 1);
            commService = new SerialCommService();
            commService.Received += this.OnReceivedAsync;
            writer = new LogWriter();
            var opened = await writer.OpenAsync();
            if (opened)
            {
                SetWatcher();
                while (true)
                {
                    await Task.Delay(100);
                    if (Canceled)
                    {
                        break;
                    }
                }
                commService.Stop();
            }
            commService.Received -= this.OnReceivedAsync;
            writer.Close();
            taskInstance.Canceled -= this.OnCanceled;
            deferral.Complete();
        }

        void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
#pragma warning disable CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
            writer.WriteAsync(reason.ToString());
#pragma warning restore CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
            Canceled = true;
        }

        void SetWatcher()
        {
            watcher = DeviceInformation.CreateWatcher(SerialDevice.GetDeviceSelector());
            watcher.Added += OnAddedAsync;
            watcher.Removed += OnRemovedAsync;
            watcher.Updated += OnUpdatedAsync;
            watcher.EnumerationCompleted += OnEnumerationCompletedAsync;
            watcher.Start();
        }

        async void OnAddedAsync(DeviceWatcher sender, DeviceInformation di)
        {
            if (isEnumerated && !IsConnected)
            {
                await ConnectAsync();
            }
        }

        private async Task ConnectAsync()
        {
            semaphore.Wait();
            try
            {
                if (!IsConnected)
                {
                    var result = await commService.OpenAsync();
                    IsConnected = result;
                    if (result)
                    {
#pragma warning disable CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
                        commService.StartAsync();
#pragma warning restore CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
                        await writer.WriteAsync(string.Format("Connected: {0}", commService.Description));
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async void OnReceivedAsync(ICommService sender, MessageEventArgs args)
        {
            await writer.WriteAsync(args.Message);
        }

        private async Task DisconnectAsync()
        {
            semaphore.Wait();
            try
            {
                if (IsConnected)
                {
                    commService.Stop();
                    IsConnected = false;
                    await writer.WriteAsync(string.Format("Disconnected {0}", commService.Description));
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        async void OnUpdatedAsync(DeviceWatcher sender, DeviceInformationUpdate diu)
        {
            if (!IsConnected)
            {
                await ConnectAsync();
            }
        }

        async void OnRemovedAsync(DeviceWatcher sender, DeviceInformationUpdate di)
        {
            if (IsConnected && commService.DeviceInfo != null)
            {
                if (commService.DeviceInfo.Id.Equals(di.Id))
                {
                    await DisconnectAsync();
                }
            }
        }

        private async void OnEnumerationCompletedAsync(DeviceWatcher sender, object args)
        {
            isEnumerated = true;
            await ConnectAsync();
        }
    }
}
