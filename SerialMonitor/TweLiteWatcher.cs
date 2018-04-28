using System;
using System.Text.RegularExpressions;
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
            await AnalyzeTagMessageAsync(args.Message);
        }

        static Regex StandardRegex = new Regex(@"::rc=(?<relay>[\dA-F]{8}):lq=(?<lqi>\d+):ct=(?<ct>[\dA-F]{4}):ed=(?<addr>[\dA-F]{8}):id=(?<id>[\dA-F]+):ba=(?<batt>\d+):a1=(?<adc1>\d+):a2=(?<adc2>\d+):x=(?<x>-?\d+):y=(?<y>-?\d+):z=(?<z>-?\d+)$");
        static Regex SmplTag3Regex = new Regex(@";(?<ts>\d+);(?<rcvr>[\dA-F]+);(?<lqi>\d+);(?<ct>[\dA-F]+);(?<serial>[\da-f]+);(?<batt>\d+);(?<mode>[\da-f]+);0000;(?<adc1>\d+);(?<adc2>\d+);X;(?<x>-?\d+);(?<y>-?\d+);(?<z>-?\d+);");
        static Regex AsciiRegex = new Regex(@":(?<rcvr>[\dA-F]{8})(?<lqi>[\dA-F]{2})(?<ct>[\dA-F]{3})(?<sid>[\dA-F]{8})(?<pkt>[\dA-F]{2})(?<batt>[\dA-F]{2})(?<adc1>[\dA-F]{4})(?<adc2>[\dA-F]{4})(?<mode>[\dA-F]{2})(?<x>[\dA-F]{4})(?<y>[\dA-F]{4})(?<z>[\dA-F]{4})");

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// normal style
        /// ::rc=80000000:lq=84:ct=0069:ed=810F1D2F:id=0:ba=2760:a1=0984:a2=0571:x=0000:y=-097:z=-045
        /// semicolon style
        /// ;23;00000000;141;110;10f1d2f;2760;0008;0000;1025;0586;X;-005;-093;-019;
        /// ascii style
        /// :80000000900071810F1D2F0035A20480027C08FFFEFF9CFFDB50
        private async Task AnalyzeTagMessageAsync(string msg)
        {
            Match m = null;
            if (msg.StartsWith("::"))
            {
                m = StandardRegex.Match(msg);
                if (m.Success)
                {
                    await WOLHelper.WakeUpAllAsync();
                }
            }
            else if (msg.StartsWith(";"))
            {
                m = SmplTag3Regex.Match(msg);
                if (m.Success)
                {
                    await WOLHelper.WakeUpAllAsync();
                }

            }
            else if (msg.StartsWith(":"))
            {
                m = AsciiRegex.Match(msg);
                if (m.Success)
                {
                    await WOLHelper.WakeUpAllAsync();
                }
            }
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
