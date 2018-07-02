using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using System;
using WakeOnDoor.Services;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Xaml.Controls;

namespace WakeOnDoor.ViewModels
{
    public class NavigationPageViewModel : ViewModelBase
    {
        public bool IsIoTDeviceFamily { get { return App.IsIoTDeviceFamily; } }

        public INavigationService NavigationService { get; set; }

        public ContentDialog PowerDialog { get; set; }

        private string currentPage;
        public string CurrentPage
        {
            set
            {
               switch (value)
                {
                    case PageTokens.ShutdownDialog:
                        ShowPowerDialog();
                        break;
                    default:
                        if (currentPage != value)
                        {
                            currentPage = value;
                            NavigationService.Navigate(value, null);
                        }
                        break;
                }
            }
        }

        public NavigationPageViewModel()
        {
        }

        public async void ShowPowerDialog()
        {
            var result = await PowerDialog.ShowAsync();
            if (result == ContentDialogResult.Primary && IsIoTDeviceFamily)
            {
                await LogReceiveServer.GetInstance().DisconnectAsync();
                ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(0));
            }
        }
    }
}
