using WakeOnDoor.ViewModels;
using Windows.UI.Xaml.Controls;

namespace WakeOnDoor.Views
{
    /// <summary>
    /// 設定ページ
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPageViewModel ViewModel => this.DataContext as SettingsPageViewModel;
        public SettingsPage()
        {
            this.InitializeComponent();
            ViewModel.Dispatcher = this.Dispatcher;
        }
    }
}
