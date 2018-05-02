using Prism.Windows.AppModel;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using System;
using System.Linq;
using WakeOnDoor.ViewModels;
using Windows.UI.Xaml.Controls;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace WakeOnDoor.Views
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class NavigationPage : Page
    {
        public NavigationPageViewModel ViewModel => this.DataContext as NavigationPageViewModel;
        public NavigationPage()
        {
            this.InitializeComponent();
            DataContextChanged += (sender, args) =>
            {
                ViewModel.NavigationService = new FrameNavigationService(
                    new FrameFacadeAdapter(ContentFrame),
                    s => Type.GetType(this.GetType().Namespace + $".{s}Page"),
                    new SessionStateService());
            };
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                ContentFrame.Navigate(typeof(TargetEditorPage));
            }
            else if (ViewModel != null)
            {
                // find NavigationViewItem with Content that equals InvokedItem
                var item = sender.MenuItems.OfType<NavigationViewItem>().First(x => (string)x.Content == (string)args.InvokedItem);
                ViewModel.CurrentPage = item.Tag.ToString();
            }
        }
    }
}
