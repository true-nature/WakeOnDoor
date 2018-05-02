using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WakeOnDoor.Views;

namespace WakeOnDoor.ViewModels
{
    public class NavigationPageViewModel : ViewModelBase
    {
        public NavigationPageViewModel()
        {
        }

        private INavigationService navigationService;
        public INavigationService NavigationService { get
            {
                return navigationService;
            }
            set
            {
                SetProperty(ref navigationService, value);
            }
        }

        private string currentPage;
        public string CurrentPage
        {
            get
            { return currentPage; }
            set
            {
                SetProperty(ref currentPage, value);
                NavigationService.Navigate(value, null);
            }
        }
    }
}
