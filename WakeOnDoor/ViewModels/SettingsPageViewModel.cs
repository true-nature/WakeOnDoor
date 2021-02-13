using Prism.Commands;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using WakeOnDoor.Models;
using Windows.UI.Core;

namespace WakeOnDoor.ViewModels
{
    public class SettingsPageViewModel : ViewModelBase
    {
        private readonly TargetEditorModel EditorModel;
        private readonly Dictionary<string, string> languages;
        public SettingsPageViewModel()
        {
            languages = new Dictionary<string, string>()
            {
                { "en-US", "English" },
                { "ja-JP", "Japanese" }
            };
            EditorModel = TargetEditorModel.GetInstance();
            selectedKey = EditorModel.Language;
            ApplyCommand = new DelegateCommand(async () => {
                EditorModel.Language = selectedKey;
                await EditorModel.SetIntervalAsync(intervalSec);
            });
            CancelCommand = new DelegateCommand(() =>
            {
                // overwrite to raise propertychanged event
                EditorModel.Language = EditorModel.Language;
            });
        }

        public ICommand ApplyCommand { get; }
        public ICommand CancelCommand { get; }

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

        private CoreDispatcher dispatcher;
        public CoreDispatcher Dispatcher
        {
            get { return dispatcher; }
            set
            {
                dispatcher = value;
            }
        }

        private async void OnModelPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            switch (args.PropertyName)
            {
                case nameof(EditorModel.IntervalSecStr):
                    Int32.TryParse(EditorModel.IntervalSecStr, out intervalSec);
                    await Dispatcher.TryRunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        RaisePropertyChanged(nameof(IntervalSec));
                    });
                    break;
                default:
                    break;
            }
        }
        public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);
            IntervalSec = await EditorModel.GetIntervalAsync();
            EditorModel.PropertyChanged += OnModelPropertyChanged;
            await EditorModel.GetListAsync();
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            base.OnNavigatingFrom(e, viewModelState, suspending);
            EditorModel.PropertyChanged -= OnModelPropertyChanged;
        }

    }
}
