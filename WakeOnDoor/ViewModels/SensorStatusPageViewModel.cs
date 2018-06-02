using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using SerialMonitor.Scanner;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WakeOnDoor.Models;
using Windows.UI.Core;

namespace WakeOnDoor.ViewModels
{
    public class SensorStatusPageViewModel: ViewModelBase
    {
        private const ushort LOW_BATTERY_LEVEL = 2400;
        private PacketLogModel LogModel;

        private TagInfo tag;
        public TagInfo Tag
        {
            get { return tag; }
            set
            {
                SetProperty(ref tag, value);
            }
        }

        private bool isLowBattery;
        public bool IsLowBattery
        {
            get
            {
                return isLowBattery;
            }
            set
            {
                SetProperty(ref isLowBattery, value);
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

        public SensorStatusPageViewModel()
        {
            LogModel = PacketLogModel.GetInstance();
            tag = LogModel.Tag;
        }

        private async void OnModelPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            switch (args.PropertyName)
            {
                case nameof(LogModel.Tag):
                    await Dispatcher.TryRunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        Tag = LogModel.Tag;
                        IsLowBattery = Tag.Batt < LOW_BATTERY_LEVEL;
                    });
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
