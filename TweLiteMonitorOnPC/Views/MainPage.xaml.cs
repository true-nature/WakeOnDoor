using TweLiteMonitorOnPC.ViewModels;
using Windows.UI.Xaml.Controls;

namespace TweLiteMonitorOnPC.Views
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var ViewModel = this.DataContext as MainPageViewModel;
            ViewModel.OnLoad();
        }
    }
}
