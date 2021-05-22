using Prism.Mvvm;
using Prism.Regions;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using SerialMonitor.Scanner;
using System;
using WakeOnDoor.Models;

namespace WakeOnDoor.ViewModels
{
    public class SensorStatusPageViewModel: BindableBase, INavigationAware, IDisposable
    {
        private const ushort LOW_BATTERY_LEVEL = 2700;

        public ReadOnlyReactivePropertySlim<TagInfo> Tag { get; }

        public ReactivePropertySlim<bool> IsLowBattery { get; } = new ReactivePropertySlim<bool>(false);

        public SensorStatusPageViewModel(PacketLogModel logModel)
        {
            Tag = logModel.ObserveProperty(x => x.Tag).ToReadOnlyReactivePropertySlim();
        }

        private void OnTagChanged(TagInfo tag)
        {
            IsLowBattery.Value = tag.Batt < LOW_BATTERY_LEVEL;
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool flag)
        {
            Tag.Dispose();
            IsLowBattery.Dispose();
        }
    }
}
