using GalaSoft.MvvmLight.Command;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using Reactive.Bindings;
using System;
using System.Windows.Input;
using WakeOnDoor.Services;
using WakeOnDoor.Views;
using Windows.ApplicationModel.Resources;
using Windows.System;
using Windows.UI.Xaml.Controls;

namespace WakeOnDoor.ViewModels
{
    public class NavigationPageViewModel : BindableBase
    {
        private readonly IRegionManager RegionManager;
        private readonly ICommService CommService;

        public bool IsIoTDeviceFamily { get { return App.IsIoTDeviceFamily; } }

        public ReactivePropertySlim<string> CurrentPage { get; } = new ReactivePropertySlim<string>();

        public ContentDialog ShutdownDialog { get; set; }
        public ContentDialog RestartDialog { get; set; }

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

        public NavigationPageViewModel(IRegionManager regionManager, IContainerProvider container, ICommService commService)
        {
            RegionManager = regionManager;
            CommService = commService;
            ShutdownDialog = container.Resolve<ShutdownDialog>();
            RestartDialog = container.Resolve<RestartDialog>();
            ItemInvokedCommand = new RelayCommand<NavigationViewItemInvokedEventArgs>(OnItemInvoked);
        }

        private void OnItemInvoked(NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                RegionManager.RequestNavigate("ContenetRegion", PageTokens.SettingsPage);
                CurrentPage.Value = PageTokens.SettingsPage;
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
                        default:
                                RegionManager.RequestNavigate("ContenetRegion", tag);
                                var resourceLoader = ResourceLoader.GetForCurrentView();
                                Title = resourceLoader.GetString(tag + "/Text");
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
