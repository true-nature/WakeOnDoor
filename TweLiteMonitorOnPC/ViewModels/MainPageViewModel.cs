using Reactive.Bindings;
using SerialMonitor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.System.Profile;

namespace TweLiteMonitorOnPC.ViewModels
{
    public class MainPageViewModel
    {
        public MainPageViewModel()
        {            
        }

        private const int LOG_CAPACITY = 40;
        private readonly List<string> textLog = new List<string>();

        public ReactiveProperty<string> LogText { get; } = new ReactiveProperty<string>(String.Empty);

        private void AppendLog(string message)
        {
            while (textLog.Count > LOG_CAPACITY)
            {
                textLog.RemoveAt(0);
            }
            textLog.Add(message);
            LogText.Value = string.Join("\n", textLog);
        }

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
                AppendLog("BackgroundTask Completed");
                if (!InPage) return;
                // restart when ExecutionTimeExceeded
                var restarted = await trigger.RequestAsync();
                AppendLog(string.Format("Restart BackgroundTask: {0}", restarted));
            };
            var result = await trigger.RequestAsync();
            AppendLog(string.Format("Start BackgroundTask: {0}", result));
            return result;
        }

        public async void OnLoad()
        {
            InPage = true;
            if (!"Windows.IoT".Equals(AnalyticsInfo.VersionInfo.DeviceFamily) && taskRegistration == null)
            {
                await StarTaskAsync();
            }
        }

        public void OnClosing()
        {
            InPage = false;
            taskRegistration?.Unregister(true);
            taskRegistration = null;
        }
    }
}
