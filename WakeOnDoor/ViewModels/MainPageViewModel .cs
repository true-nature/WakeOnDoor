using Prism.Commands;
using Prism.Windows.Mvvm;
using SerialMonitor;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WakeOnDoor.Services;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace WakeOnDoor.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {

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
                if (appConnection == null) { return; }
                var values = new ValueSet
                {
                    [nameof(Keys.Command)] = nameof(AppCommands.Add),
                    [nameof(Keys.MacAddress)] = MacToAdd
                };
                var response = await appConnection.SendMessageAsync(values);
                if (response.Status == AppServiceResponseStatus.Success)
                {
                    var list = response.Message[nameof(Keys.MacList)] as string[];
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
            InitMacListAsync();
            ConnectAsync();
#pragma warning restore CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
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

        private async Task InitMacListAsync()
        {
            var conn = new AppServiceConnection
            {
                AppServiceName = "SettingsEditor",
                PackageFamilyName = "TweLiteMonitor-uwp_13vwvk7mqd5qw"
            };
            var status = await conn.OpenAsync();
            if (status == AppServiceConnectionStatus.Success)
            {
                appConnection = conn;
                ValueSet request = new ValueSet();
                var response = await appConnection.SendMessageAsync(request);
                if (response.Status == AppServiceResponseStatus.Success)
                {
                    var list = response.Message[nameof(Keys.MacList)] as string[];
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
