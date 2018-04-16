using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.UI.Core;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace TweLiteMonitor
{
    public sealed class StartupTask : IBackgroundTask
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
            //LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Trace, LogLevel.Fatal, new FileStreamingTarget());
            //ILogger logger = LogManagerFactory.DefaultLogManager.GetLogger<TweLiteMonitor>();
            //logger.Trace("Run()");
            /// TODO: code here
            taskInstance.Canceled += this.OnCanceled;
            IsConnected = false;
            semaphore = new SemaphoreSlim(1, 1);
            SetWatcher();
            writer = new LogWriter();
            commService = new SerialCommService();
            commService.Received += this.OnReceived;
            var opened = await writer.OpenAsync();
            if (opened)
            {
                while (true)
                {
                    if (Canceled)
                    {
                        break;
                    }
                }
            }
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
            if (isEnumerated)
            {
                await writer.WriteAsync(string.Format("Added: {0}", di.Id));
                await ConnectAsync();
            }
        }

        private async Task ConnectAsync()
        {
            semaphore.Wait();
            if (!IsConnected)
            {
                var result = await commService.OpenAsync();
                IsConnected = result;
                if (result)
                {
                    await writer.WriteAsync(string.Format("Connected: {0}", commService.Description));
#pragma warning disable CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
                    commService.StartAsync();
#pragma warning restore CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
                }
            }
            semaphore.Release();
        }

        private void OnReceived(ICommService sender, MessageEventArgs args)
        {
#pragma warning disable CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
            writer.WriteAsync(args.Message);
#pragma warning restore CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
        }

        private async Task DisconnectAsync()
        {
            semaphore.Wait();
            if (IsConnected)
            {
                await writer.WriteAsync(string.Format("Disconnected {0}", commService.Description));
                commService.Stop();
                commService.Close();
                IsConnected = false;
            }
            semaphore.Release();
        }

        async void OnUpdatedAsync(DeviceWatcher sender, DeviceInformationUpdate diu)
        {
            await writer.WriteAsync(string.Format("Updated: {0}", diu.Id));
            await ConnectAsync();
        }

        async void OnRemovedAsync(DeviceWatcher sender, DeviceInformationUpdate di)
        {
            await writer.WriteAsync(string.Format("Removed: {0}", di.Id));
            if (IsConnected && commService.DeviceInfo != null)
            {
                if (commService.DeviceInfo.Id.Equals(di.Id))
                {
                    await DisconnectAsync();
                }
            }
        }

        async void OnEnumerationCompletedAsync(DeviceWatcher sender, object args)
        {
            isEnumerated = true;
            await ConnectAsync();
        }
    }
}
