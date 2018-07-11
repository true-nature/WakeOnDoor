using WakeOnDoor.ViewModels;
using Windows.UI.Xaml.Controls;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace WakeOnDoor.Views
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
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
