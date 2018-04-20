using Prism.Commands;
using Prism.Windows.Mvvm;
using Prism.Windows.Validation;
using SerialMonitor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
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
    public class MainPageViewModel : ValidatableBindableBase
    {
        private const int LOG_CAPACITY = 50;
        public MainPageViewModel()
        {
            IsIoTDeviceFamily = ("Windows.IoT".Equals(AnalyticsInfo.VersionInfo.DeviceFamily));
            WOLTargets = new ObservableCollection<WOLTarget>();
            IsConnected = false;
            textLog = new List<string>();
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
                textLog.Clear();
                RaisePropertyChanged(nameof(TextLog));
            });
            this.AddMacCommand = new DelegateCommand(async () => {
                using (var conn = await OpenAppServiceAsync())
                {
                    if (conn == null) { return; }
                    var values = new ValueSet
                    {
                        [nameof(Keys.Command)] = nameof(AppCommands.Add),
                        [nameof(Keys.MacAddress)] = PhysicalToEdit,
                        [nameof(Keys.Comment)] = CommentToEdit
                    };
                    var response = await conn.SendMessageAsync(values);
                    if (response.Status == AppServiceResponseStatus.Success)
                    {
                        var targetListStr = response.Message[nameof(Keys.TargetList)] as string;
                        RefreshTargetList(targetListStr);
                    }
                }
            });
            this.RemoveMacCommand = new DelegateCommand(async () => {
                using (var conn = await OpenAppServiceAsync())
                {
                    if (conn == null) { return; }
                    var values = new ValueSet
                    {
                        [nameof(Keys.Command)] = nameof(AppCommands.Remove),
                        [nameof(Keys.MacAddress)] = PhysicalToEdit,
                        [nameof(Keys.Comment)] = CommentToEdit
                    };
                    var response = await conn.SendMessageAsync(values);
                    if (response.Status == AppServiceResponseStatus.Success)
                    {
                        var targetListStr = response.Message[nameof(Keys.TargetList)] as string;
                        RefreshTargetList(targetListStr);
                    }
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
            InitTargetListAsync();
            ConnectAsync();
#pragma warning restore CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
        }

        public bool IsIoTDeviceFamily { get; }
        public string StatusMessage { get; private set; }

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
        public ICommand RemoveMacCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand ExitCommand { get; }

        private string physicalToEdit;
        public string PhysicalToEdit
        {
            get { return physicalToEdit; }
            set { SetProperty(ref physicalToEdit, value, nameof(PhysicalToEdit)); }
        }
        private string commentToEdit;
        public string CommentToEdit
        {
            get { return commentToEdit; }
            set { SetProperty(ref commentToEdit, value, nameof(CommentToEdit)); }
        }

        public ObservableCollection<WOLTarget> WOLTargets { get; }

        public WOLTarget SelectedTarget
        {
            set
            {
                if (!string.IsNullOrEmpty(value?.Physical))
                {
                    PhysicalToEdit = value?.Physical;
                    CommentToEdit = value?.Comment;
                }
            }
        }

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

        private List<string> textLog;
        public string TextLog
        {
            get { return string.Join("\n", textLog); }
        }

        private void RefreshTargetList(string targetJsonStr)
        {
            using (var memStream = new MemoryStream(Encoding.UTF8.GetBytes(targetJsonStr)))
            {
                var serializer = new DataContractJsonSerializer(typeof(HashSet<WOLTarget>));
                var targets = serializer.ReadObject(memStream) as HashSet<WOLTarget>;
                WOLTargets.Clear();
                foreach (var m in targets)
                {
                    WOLTargets.Add(m);
                }
                RaisePropertyChanged(nameof(WOLTargets));
            }
        }

        private async Task InitTargetListAsync()
        {
            using (var conn = await OpenAppServiceAsync())
            {
                if (conn == null) { return; }
                ValueSet request = new ValueSet();
                var response = await conn.SendMessageAsync(request);
                if (response.Status == AppServiceResponseStatus.Success)
                {
                    var targetListStr = response.Message[nameof(Keys.TargetList)] as string;
                    RefreshTargetList(targetListStr);
                }
            }
        }

        private async Task<AppServiceConnection> OpenAppServiceAsync()
        {
            var conn = new AppServiceConnection
            {
                AppServiceName = "SettingsEditor",
                PackageFamilyName = "TweLiteMonitor-uwp_13vwvk7mqd5qw"
            };
            var status = await conn.OpenAsync();
            if (status == AppServiceConnectionStatus.Success)
            {
                return conn;
            }
            return null;
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
                while (textLog.Count > LOG_CAPACITY)
                {
                    textLog.RemoveAt(0);
                }
                textLog.Add(args.Message);
                RaisePropertyChanged(nameof(TextLog));
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
