using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using SerialMonitor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.System.Profile;
using Windows.UI.Core;

namespace TweLiteMonitorOnPC.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private const int LOG_CAPACITY = 40;
        public MainPageViewModel()
        {            
        }

        private List<string> textLog = new List<string>();
        public string TextLog
        {
            get { return string.Join("\n", textLog); }
        }

        private async Task AppendLogAsync(string message)
        {
            await Dispatcher.TryRunAsync(CoreDispatcherPriority.Normal, () =>
             {
                 while (textLog.Count > LOG_CAPACITY)
                 {
                     textLog.RemoveAt(0);
                 }
                 textLog.Add(message);
                 RaisePropertyChanged(nameof(TextLog));
             });
        }
        public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            InPage = true;
            if (!"Windows.IoT".Equals(AnalyticsInfo.VersionInfo.DeviceFamily) && taskRegistration == null)
            {
                await StarTaskAsync();
            }
            base.OnNavigatedTo(e, viewModelState);
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            InPage = false;
            taskRegistration?.Unregister(true);
            taskRegistration = null;
            base.OnNavigatingFrom(e, viewModelState, suspending);
        }

        public CoreDispatcher Dispatcher { get; set; }

        private bool InPage { get; set; }
        private BackgroundTaskRegistration taskRegistration;
        private async Task<ApplicationTriggerResult> StarTaskAsync()
        {
            const string TaskName = "TweLiteMonitor-uwp";
            foreach (var t in BackgroundTaskRegistration.AllTasks)
            {
                if (t.Value.Name == TaskName)
                {
                    t.Value.Unregister(true);
                    //return ApplicationTriggerResult.CurrentlyRunning;
                }
            }
            var builder = new BackgroundTaskBuilder
            {
                Name = TaskName,
                TaskEntryPoint = typeof(MonitorTask).FullName
            };
            var trigger = new ApplicationTrigger();
            builder.SetTrigger(trigger);
            await BackgroundExecutionManager.RequestAccessAsync();
            taskRegistration = builder.Register();
            taskRegistration.Completed += async (sender, args) =>
            {
                await AppendLogAsync("BackgroundTask Completed");
                if (!InPage) return;
                // restart when ExecutionTimeExceeded
                var restarted = await trigger.RequestAsync();
                await AppendLogAsync(string.Format("Restart BackgroundTask: {0}", restarted));
            };
            var result = await trigger.RequestAsync();
            await AppendLogAsync(string.Format("Start BackgroundTask: {0}", result));
            return result;
        }
    }
}
