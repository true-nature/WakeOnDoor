using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using System;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Xaml.Controls;

namespace WakeOnDoor.ViewModels
{
    public class NavigationPageViewModel : ViewModelBase
    {
        private const string SHUTDOWN = "Shutdown";

        public bool IsIoTDeviceFamily { get; }

        private INavigationService _navigationService;
        public INavigationService NavigationService
        {
            get
            {
                return _navigationService;
            }
            set
            {
                SetProperty(ref _navigationService, value);
            }
        }
        public ContentDialog PowerDialog { get; set; }

        private string currentPage;
        public string CurrentPage
        {
            get
            { return currentPage; }
            set
            {
                SetProperty(ref currentPage, value);
                switch (value)
                {
                    case SHUTDOWN:
                        ShowPowerDialog();
                        break;
                    default:
                        _navigationService.Navigate(value, null);
                        break;
                }
            }
        }

        public NavigationPageViewModel(INavigationService navigationService)
        {
            IsIoTDeviceFamily = ("Windows.IoT".Equals(AnalyticsInfo.VersionInfo.DeviceFamily));
            _navigationService = navigationService;
        }

        public async void ShowPowerDialog()
        {
            var result = await PowerDialog.ShowAsync();
            if (result == ContentDialogResult.Primary && IsIoTDeviceFamily)
            {
                ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(0));
            }
        }
    }
}
