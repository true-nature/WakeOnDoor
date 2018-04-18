using Nito.AsyncEx;
using Prism.Commands;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WakeOnDoor.Services;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace WakeOnDoor.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private const string KEY_COMMAND = "Command";
        private const string KEY_MACLIST = "MacList";
        private const string KEY_MAC_ADDRESS = "MacAddress";
        private const string KEY_RESULT = "Result";
        private const string CMD_ADD = "Add";
        private const string CMD_REMOVE = "Remove";
        private const string CMD_GET = "Get";

        public MainPageViewModel()
        {
            IsIoTDeviceFamily = ("Windows.IoT".Equals(AnalyticsInfo.VersionInfo.DeviceFamily));
            IsConnected = false;
            textLog = "";
            this.StatusViewCommand = new DelegateCommand(() =>
            {
                IsMacVisible = false;
                IsStatusVisible = true;
            });
            this.MacViewCommand = new DelegateCommand(() =>
            {
                IsStatusVisible = false;
                IsMacVisible = true;
            });
            this.ClearLogCommand = new DelegateCommand(() =>
            {
                TextLog = "";
            });
            this.AddMacCommand = new DelegateCommand(async () => {
                var values = new ValueSet();
                values[KEY_COMMAND] = CMD_ADD;
                values[KEY_MAC_ADDRESS] = MacToAdd;
                var response = await appConnection.SendMessageAsync(values);
                if (response.Status == AppServiceResponseStatus.Success)
                {
                    var list = response.Message[KEY_MACLIST] as string[];
                    RefreshMacList(list);
                }
            });
            this.ExitCommand = new DelegateCommand(async () =>
            {
                await DisconnectAsync();
                Application.Current.Exit();
            });
            IsMacVisible = false;
            IsStatusVisible = true;
            semaphore = new SemaphoreSlim(1, 1);
            commService = new LogReceiveServer();
            commService.Received += this.OnReceived;
#pragma warning disable CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
            this.ConnectAsync();
#pragma warning restore CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
            initMacListAsync();
        }

        private AppServiceConnection appConnection;
        public bool IsIoTDeviceFamily { get; }
        private SemaphoreSlim semaphore;
        public ICommand MacViewCommand { get; }
        private bool MacViewVisibility;
        public bool IsMacVisible
        {
            get {
                return MacViewVisibility;
            }
            private set
            {
                SetProperty(ref MacViewVisibility, value, nameof(IsMacVisible));
            }
        }
        public ICommand StatusViewCommand { get; }
        private bool StatusViewVisibility;
        public bool IsStatusVisible
        {
            get { return StatusViewVisibility; }
            private set
            {
                SetProperty(ref StatusViewVisibility, value, nameof(IsStatusVisible));
            }
        }
        public ICommand AddMacCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand ExitCommand { get; }

        private string macToAdd;
        public string MacToAdd
        {
            get { return macToAdd; }
            set { SetProperty(ref macToAdd, value, nameof(MacToAdd)); }
        }
        public ICollection<string> MacList { get; }

        private CoreDispatcher dispatcher;
        public CoreDispatcher Dispatcher
        {
            get { return dispatcher; }
            set
            {
                dispatcher = value;
            }
        }
        private ICommService commService;

        private bool isConnected;
        public bool IsConnected
        {
            get { return this.isConnected; }
            set
            {
                this.SetProperty(ref this.isConnected, value, nameof(IsConnected));
            }
        }

        private string textLog;
        public string TextLog
        {
            get { return this.textLog; }
            set
            {
                this.SetProperty(ref this.textLog, value);
                RaisePropertyChanged(nameof(TextLog));
            }
        }

        private void RefreshMacList(string[] list)
        {
            MacList.Clear();
            foreach (var m in list)
            {
                MacList.Add(m);
            }
        }

        private async Task initMacListAsync()
        {
            var conn = new AppServiceConnection();
            conn.AppServiceName = "TweLiteMonitor";
            //conn.PackageFamilyName = "TweLiteMonitor-uwp_mtz6gfc7cpfh4";
            conn.PackageFamilyName = "23e95b2f-1e21-4934-a371-124d0a6471a0_mtz6gfc7cpfh4";
            var mre = new AsyncManualResetEvent(false);
            var op = conn.OpenAsync();
            op.Completed += (sender, args) => { mre.Set(); };
            await mre.WaitAsync();
            var status = op.GetResults();
            if (status == AppServiceConnectionStatus.Success)
            {
                appConnection = conn;
                ValueSet request = new ValueSet();
                var response = await appConnection.SendMessageAsync(request);
                if (response.Status == AppServiceResponseStatus.Success)
                {
                    var list = response.Message[KEY_MACLIST] as string[];
                    RefreshMacList(list);
                }
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
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private void OnReceived(ICommService sender, MessageEventArgs args)
        {
#pragma warning disable CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
            Dispatcher.TryRunAsync(CoreDispatcherPriority.Normal,() =>
            {
                TextLog += args.Message + "\n";
            });
#pragma warning restore CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
        }

        private async Task DisconnectAsync()
        {
            await semaphore.WaitAsync();
            try
            {
                if (IsConnected)
                {
                    commService.Stop();
                    commService.Close();
                    IsConnected = false;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
