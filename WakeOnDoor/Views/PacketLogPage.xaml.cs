using WakeOnDoor.ViewModels;
using Windows.UI.Xaml.Controls;

namespace WakeOnDoor.Views
{
    /// <summary>
    /// ログ表示ページ
    /// </summary>
    public sealed partial class PacketLogPage : Page
    {
        public PacketLogPageViewModel ViewModel => this.DataContext as PacketLogPageViewModel;
        public PacketLogPage()
        {
            this.InitializeComponent();
            ViewModel.Dispatcher = this.Dispatcher;
        }

        private void LogBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LogScroller.ChangeView(0.0f, LogScroller.ExtentHeight, 1.0f);
        }
    }
}
