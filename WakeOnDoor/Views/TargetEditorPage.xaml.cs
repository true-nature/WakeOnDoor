using SerialMonitor;
using WakeOnDoor.ViewModels;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace WakeOnDoor.Views
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class TargetEditorPage : Page
    {
        public TargetEditorPageViewModel ViewModel => this.DataContext as TargetEditorPageViewModel;
        public TargetEditorPage()
        {
            this.InitializeComponent();
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedTarget = TargetListView.SelectedItem as WOLTarget;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values[TargetEditorPageViewModel.TEMP_PREFIX + nameof(ViewModel.PhysicalToEdit)] = PhysicalTextBox.Text;
            settings.Values[TargetEditorPageViewModel.TEMP_PREFIX + nameof(ViewModel.AddressToEdit)] = AddressTextBox.Text;
            settings.Values[TargetEditorPageViewModel.TEMP_PREFIX + nameof(ViewModel.PortToEdit)] = PortTextBox.Text;
            settings.Values[TargetEditorPageViewModel.TEMP_PREFIX + nameof(ViewModel.DelayToEdit)] = DelayTextBox.Text;
            settings.Values[TargetEditorPageViewModel.TEMP_PREFIX + nameof(ViewModel.CommentToEdit)] = CommentTextBox.Text;
            base.OnNavigatingFrom(e);
        }
    }
}
