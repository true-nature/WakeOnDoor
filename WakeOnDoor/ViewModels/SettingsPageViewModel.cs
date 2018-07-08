using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WakeOnDoor.ViewModels
{
    public class SettingsPageViewModel : ViewModelBase
    {
        public SettingsPageViewModel()
        {
        }

        public ICommand SetIntervalCommand { get; }
        public ICommand SetLanguageCommand { get; }

        private int intervalSec;
        public int IntervalSec
        {
            get => intervalSec;
            set => SetProperty(ref intervalSec, value);
        }

    }
}
