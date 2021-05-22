using System;
using WakeOnDoor.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace WakeOnDoor.Views
{
    /// <summary>
    /// ナビゲーションページ
    /// </summary>
    public sealed partial class NavigationPage : Page
    {
        public NavigationPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        public void OnLoaded(object sender, RoutedEventArgs args)
        {
            var vm = DataContext as NavigationPageViewModel;
            vm.CurrentPage.Subscribe(OnPageSelected);
        }

        private void OnPageSelected(Type pageType)
        {
            ContentFrame.Navigate(pageType ?? typeof(SensorStatusPage));
        }
    }
}
