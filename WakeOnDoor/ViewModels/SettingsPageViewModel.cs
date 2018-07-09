using Prism.Commands;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WakeOnDoor.Models;

namespace WakeOnDoor.ViewModels
{
    public class SettingsPageViewModel : ViewModelBase
    {
        private TargetEditorModel EditorModel;
        private Dictionary<string, string> languages;
        public SettingsPageViewModel()
        {
            languages = new Dictionary<string, string>()
            {
                { "en-US", "English" },
                { "ja-JP", "Japanese" }
            };
            EditorModel = TargetEditorModel.GetInstance();
            selectedKey = EditorModel.Language;
            // TODO IntervalSec
            ApplyCommand = new DelegateCommand(async () => {
                EditorModel.Language = selectedKey;
                await EditorModel.SetInterval(intervalSec);
            });
        }

        public ICommand ApplyCommand { get; }

        private int intervalSec;
        public int IntervalSec
        {
            get => intervalSec;
            set => SetProperty(ref intervalSec, value);
        }

        private string selectedKey;
        public string SelectedKey
        {
            get => selectedKey;
            set => SetProperty(ref selectedKey, value);
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
