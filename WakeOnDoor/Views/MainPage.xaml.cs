using SerialMonitor;
using WakeOnDoor.ViewModels;
using Windows.UI.Xaml.Controls;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace WakeOnDoor.Views
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPageViewModel ViewModel => this.DataContext as MainPageViewModel;
        public MainPage()
        {
            this.InitializeComponent();
            ViewModel.Dispatcher = this.Dispatcher;
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedTarget = TargetListView.SelectedItem as WOLTarget;
        }
    }
}
