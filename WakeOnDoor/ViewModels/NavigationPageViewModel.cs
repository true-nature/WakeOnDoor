using GalaSoft.MvvmLight.Command;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using Reactive.Bindings;
using System;
using System.Windows.Input;
using WakeOnDoor.Services;
using WakeOnDoor.Views;
using Windows.System;
using Windows.UI.Xaml.Controls;

namespace WakeOnDoor.ViewModels
{
    public class NavigationPageViewModel : BindableBase
    {
        private readonly ICommService CommService;

        public bool IsIoTDeviceFamily { get { return App.IsIoTDeviceFamily; } }

        public ReactivePropertySlim<Type> CurrentPage { get; } = new ReactivePropertySlim<Type>(typeof(SensorStatusPage));

        private ContentDialog ShutdownDialog { get; set; }
        private ContentDialog RestartDialog { get; set; }

        private string title;
        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                SetProperty(ref title, value);
            }
        }

        public ICommand ItemInvokedCommand { get; }

        public NavigationPageViewModel(IContainerProvider container, ICommService commService)
        {
            CommService = commService;
            ShutdownDialog = container.Resolve<ShutdownDialog>();
            RestartDialog = container.Resolve<RestartDialog>();
            ItemInvokedCommand = new RelayCommand<NavigationViewItemInvokedEventArgs>(OnItemInvoked);
        }

        private void OnItemInvoked(NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                CurrentPage.Value = typeof(SettingsPage);
            }
            else
            {
                // find NavigationViewItem with Content that equals InvokedItem
                var item = args.InvokedItemContainer as NavigationViewItem;
                var tag = item?.Tag as string;
                if (!String.IsNullOrEmpty(tag))
                {
                    switch (tag)
                    {
                        case PageTokens.ShutdownDialog:
                            ShowPowerDialog(ShutdownKind.Shutdown);
                            break;
                        case PageTokens.RestartDialog:
                            ShowPowerDialog(ShutdownKind.Restart);
                            break;
                        case PageTokens.SensorStatusPage:
                            CurrentPage.Value = typeof(SensorStatusPage);
                            break;
                        case PageTokens.TargetEditorPage:
                            CurrentPage.Value = typeof(TargetEditorPage);
                            break;
                        case PageTokens.PacketLogPage:
                            CurrentPage.Value = typeof(PacketLogPage);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public async void ShowPowerDialog(ShutdownKind kind)
        {
            var dialog = (kind == ShutdownKind.Restart ? RestartDialog : ShutdownDialog);
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && IsIoTDeviceFamily)
            {
                await CommService.DisconnectAsync();
                ShutdownManager.BeginShutdown(kind, TimeSpan.FromSeconds(0));
            }
        }
    }
}
