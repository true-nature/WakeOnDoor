using Prism.Commands;
using Prism.Windows.Mvvm;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WakeOnDoor.Services;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace WakeOnDoor.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        public MainPageViewModel()
        {
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
            this.ExitCommand = new DelegateCommand(() =>
            {
                Application.Current.Exit();
            });
            IsMacVisible = false;
            IsStatusVisible = true;
            semaphore = new SemaphoreSlim(1, 1);
            commService = new SerialCommService();
            commService.Received += this.OnReceived;
        }
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
        public ICommand ClearLogCommand { get; }

        public ICommand ExitCommand { get; }

        private CoreDispatcher dispatcher;
        public CoreDispatcher Dispatcher
        {
            get { return dispatcher; }
            set
            {
                dispatcher = value;
                if (dispatcher != null && watcher == null)
                {
                    SetWatcher();
                }
            }
        }
        private static DeviceWatcher watcher = null;
        private static bool isEnumerated = false;
        private ISerialCommService commService;

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
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
                {
                    TextLog += string.Format("Added: {0}\n", di.Id);
                    await ConnectAsync();
                });
            }
        }

        private async Task ConnectAsync()
        {
            semaphore.Wait();
            if (!IsConnected) { 
                var result = await commService.OpenAsync();
                IsConnected = result;
                if (result)
                {
                    var msg = string.Format("Connected: {0}\n", commService.Description);
                    TextLog += msg;
#pragma warning disable CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
                    commService.StartAsync();
#pragma warning restore CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
                }
            }
            semaphore.Release();
        }

        private void OnReceived(ICommService sender, MessageEventArgs args)
        {
            TextLog += args.Message + "\n";
        }

        private Task Disconnect()
        {
            semaphore.Wait();
            if (IsConnected)
            {
                var msg = string.Format("Disconnected {0}\n", commService.Description);
                TextLog += msg;
                commService.Stop();
                commService.Close();
                IsConnected = false;
            }
            semaphore.Release();
            return Task.CompletedTask;
        }

        async void OnUpdatedAsync(DeviceWatcher sender, DeviceInformationUpdate diu)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
            {
                TextLog += string.Format("Updated: {0}\n", diu.Id);
                await ConnectAsync();
            });
        }

        async void OnRemovedAsync(DeviceWatcher sender, DeviceInformationUpdate di)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                TextLog += string.Format("Removed: {0}\n", di.Id);
                if (IsConnected && commService.DeviceInfo != null)
                {
                    if (commService.DeviceInfo.Id.Equals(di.Id))
                    {
                        Disconnect();
                    }
                }
            });
        }

        async void OnEnumerationCompletedAsync(DeviceWatcher sender, object args)
        {
            isEnumerated = true;
            await Dispatcher?.TryRunAsync(CoreDispatcherPriority.Low, async () =>
            {
                await ConnectAsync();
            });
        }
    }
}
