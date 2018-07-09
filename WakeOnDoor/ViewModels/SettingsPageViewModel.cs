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
        private Dictionary<string, string> languages;
        public SettingsPageViewModel()
        {
            languages = new Dictionary<string, string>()
            {
                { "en-US", "English" },
                { "ja-JP", "Japanese" }
            };
        }

        public ICommand ApplyCommand { get; }

        private int intervalSec;
        public int IntervalSec
        {
            get => intervalSec;
            set => SetProperty(ref intervalSec, value);
        }

        public Dictionary<string, string> Languages
        {
            get
            {
                return languages;
            }
        }

    }
}
