using Prism.Commands;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using WakeOnDoor.Services;
using WakeOnDoor.Views;
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
            this.ExitCommand = new DelegateCommand(() =>
            {
                Application.Current.Exit();
            });
            SetWatcher();
        }
        public ICommand ExitCommand { get; }

        public CoreDispatcher Dispatcher { get; set; }
        private static DeviceWatcher watcher = null;
        private static bool isEnumerated = false;
        private ISerialCommService commService;

        public ISerialCommService SerialCommService
        {
            get { return this.commService; }
            set { this.SetProperty(ref this.commService, value); }
        }

        private bool isConnected;
        public bool IsConnected
        {
            get { return this.isConnected; }
            set
            {
                this.SetProperty(ref this.isConnected, value);
                RaisePropertyChanged(nameof(IsConnected));
                RaisePropertyChanged(nameof(IsDisonnected));
            }
        }

        public bool IsDisonnected
        {
            get { return !this.isConnected; }
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
                    if (IsDisonnected)
                    {
                        await ConnectAsync();
                    }
                });
            }
        }

        private async Task ConnectAsync()
        {
            var result = await commService.OpenAsync();
            IsConnected = result;
            if (result)
            {
                TextLog += string.Format("Connected: {0}\n", commService.DeviceInfo.Id);
            }
        }

        private Task Disconnect()
        {
            TextLog += string.Format("Disconnected {0}\n", commService.DeviceInfo.Id);
            commService.Close();
            return Task.CompletedTask;
        }

        async void OnUpdatedAsync(DeviceWatcher sender, DeviceInformationUpdate diu)
        {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
                {
                    TextLog += string.Format("Updated: {0}\n", diu.Id);
                    if (IsDisonnected)
                    {
                        await ConnectAsync();
                    }
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
            if (Dispatcher != null && Dispatcher.HasThreadAccess)
            {
                await Dispatcher.TryRunAsync(CoreDispatcherPriority.Low, async () =>
                {
                    TextLog += "OnEnumerationCompletedAsync\n";
                    if (IsDisonnected)
                    {
                        await ConnectAsync();
                    }
                });
            }
        }
    }
}
