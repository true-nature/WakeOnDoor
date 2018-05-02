using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WakeOnDoor.Views;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Xaml.Controls;

namespace WakeOnDoor.ViewModels
{
    public class NavigationPageViewModel : ViewModelBase
    {
        private const string SHUTDOWN = "Shutdown";

        public bool IsIoTDeviceFamily { get; }

        private INavigationService navigationService;
        public INavigationService NavigationService
        {
            get
            {
                return navigationService;
            }
            set
            {
                SetProperty(ref navigationService, value);
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
                        NavigationService.Navigate(value, null);
                        break;
                }
            }
        }

        public NavigationPageViewModel()
        {
            IsIoTDeviceFamily = ("Windows.IoT".Equals(AnalyticsInfo.VersionInfo.DeviceFamily));
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
