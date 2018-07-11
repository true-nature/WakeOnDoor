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
    public class PacketLogPageViewModel : ViewModelBase
    {
        private PacketLogModel LogModel;

        private const int LOG_CAPACITY = 50;

        public ICommand ClearLogCommand { get; }

        private CoreDispatcher dispatcher;
        public CoreDispatcher Dispatcher
        {
            get { return dispatcher; }
            set
            {
                dispatcher = value;
            }
        }

        private bool isLocked;
        public bool IsLocked
        {
            get { return isLocked; }
            set
            {
                SetProperty(ref isLocked, value);
                if(!isLocked)
                {
                    textLog = LogModel.LogList;
                    RaisePropertyChanged(nameof(TextLog));
                }
            }
        }

        private List<string> textLog;
        public string TextLog
        {
            get { return string.Join("\n", textLog); }
        }

        public PacketLogPageViewModel()
        {
            LogModel = PacketLogModel.GetInstance();
            textLog = LogModel.LogList;

            this.ClearLogCommand = new DelegateCommand(() =>
            {
                textLog.Clear();
                RaisePropertyChanged(nameof(TextLog));
            });
        }

        private void ScrollTo(int position)
        {

        }

        private async void OnModelPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            switch (args.PropertyName)
            {
                case nameof(LogModel.LogList):
                    if (!isLocked)
                    {
                        textLog = LogModel.LogList;
                        await Dispatcher.TryRunAsync(CoreDispatcherPriority.Normal, () =>
                          {
                              RaisePropertyChanged(nameof(TextLog));
                              // TODO: scrol to bottom automatically
                          });
                    }
                    break;
                default:
                    break;
            }
        }
        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            LogModel.PropertyChanged += OnModelPropertyChanged;
            base.OnNavigatedTo(e, viewModelState);
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            LogModel.PropertyChanged -= OnModelPropertyChanged;
            base.OnNavigatingFrom(e, viewModelState, suspending);
        }
    }
}
