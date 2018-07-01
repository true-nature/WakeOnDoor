using SerialMonitor.Scanner;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage;

namespace SerialMonitor
{
    internal class TweLiteWatcher: IDisposable
    {
        private int DEFAULT_INTERVAL_SECS = 10;
        private static DeviceWatcher watcher = null;
        private static bool isEnumerated = false;

        private SerialCommService commService;
        private bool IsConnected { get; set; }
        private SemaphoreSlim semaphore;
        private ISyslogWriter writer;

        private IEnumerable<IMessageScanner> Scanners;
        private TaskCompletionSource<bool> tcs;
        private DateTimeOffset lastWolTime;

        public TweLiteWatcher()
        {
            Scanners = new List<IMessageScanner>() { new StandardScanner(), new SmplTag3Scanner(), new AsciiScanner()};
            IsConnected = false;
            semaphore = new SemaphoreSlim(1, 1);
            commService = new SerialCommService();
            tcs = new TaskCompletionSource<bool>();
            writer = new SyslogWriter(Facility.local0, "TWELITE");
            lastWolTime = DateTimeOffset.MinValue;
        }

        public void Dispose()
        {
            tcs.TrySetCanceled();
            commService.Dispose();
            semaphore.Dispose();
            writer = null;
        }

        public async Task WatchAsync()
        {
            await writer.OpenAsync();
            commService.Received += this.OnReceivedAsync;
            SetWatcher();
            await tcs.Task;
            commService.Stop();
            commService.Received -= this.OnReceivedAsync;
        }

        public void Stop()
        {
            tcs.TrySetCanceled();
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
                        await writer.Debug(string.Format(CultureInfo.InvariantCulture, "Connected: {0}", commService.Description));
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
            var msg = args.Message;
            await writer.Notice(msg);
            foreach (var s in Scanners)
            {
                var info = s.Scan(msg);
                if (info.Valid) {
                    await writer.Info(info.ToString());
                    if (info.WolTrigger)
                    {
                        if (CheckInterval())
                        {
                            lastWolTime = DateTimeOffset.Now;
                            await writer.Debug("WOL!");
                            await WOLHelper.WakeUpAllAsync();
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if interval time has expired</returns>
        private bool CheckInterval()
        {
            var settings = ApplicationData.Current.LocalSettings;
            int interval = DEFAULT_INTERVAL_SECS;
            if (settings.Values.TryGetValue(nameof(Keys.IntervalSec), out object intervalValue))
            {
                if (int.TryParse(intervalValue as string, out interval))
                {
                    if (interval < 1)
                    {
                        interval = DEFAULT_INTERVAL_SECS;
                    }
                }
            }
            return ((DateTimeOffset.Now - lastWolTime).TotalSeconds > interval);
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
                    await writer.Debug(string.Format(CultureInfo.InvariantCulture, "Disconnected {0}", commService.Description));
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
