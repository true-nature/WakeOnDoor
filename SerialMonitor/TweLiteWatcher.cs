using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;

namespace SerialMonitor
{
    internal class TweLiteWatcher: IDisposable
    {
        private static DeviceWatcher watcher = null;
        private static bool isEnumerated = false;

        private SerialCommService commService;
        private bool IsConnected { get; set; }
        private SemaphoreSlim semaphore;
        private LogWriter writer;
        private ManualResetEvent mre;

        public TweLiteWatcher(LogWriter logWriter)
        {
            IsConnected = false;
            semaphore = new SemaphoreSlim(1, 1);
            commService = new SerialCommService();
            mre = new ManualResetEvent(false);
            writer = logWriter;
        }

        public void Dispose()
        {
            mre.Set();
            commService.Dispose();
            semaphore.Dispose();
        }

        public async Task WatchAsync()
        {
            commService.Received += this.OnReceivedAsync;
            SetWatcher();
            await Task.Run(() =>
            {
                mre.WaitOne();
            });
            commService.Stop();
            commService.Received -= this.OnReceivedAsync;
        }

        public void Stop()
        {
            mre.Set();
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
            await semaphore.WaitAsync();
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
            await semaphore.WaitAsync();
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
