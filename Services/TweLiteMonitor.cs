using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace WakeOnDoor.Services
{
    public class TweLiteMonitor : IBackgroundTask
    {
        private const string TaskName = nameof(TweLiteMonitor);
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            //LogManagerFactory.DefaultConfiguration.AddTarget(LogLevel.Trace, LogLevel.Fatal, new FileStreamingTarget());
            //ILogger logger = LogManagerFactory.DefaultLogManager.GetLogger<TweLiteMonitor>();
            //logger.Trace("Run()");
            /// TODO: code here
            deferral.Complete();
        }

        public static bool IsTaskRegistered()
        {
            var registered = false;
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == TaskName)
                {
                    registered = true;
                    break;
                }
            }
            return registered;
        }

        public static async Task StarTaskAsync()
        {
            var builder = new BackgroundTaskBuilder();
            builder.Name = TaskName;
            builder.TaskEntryPoint = nameof(WakeOnDoor.Services) + "." + nameof(TweLiteMonitor);
            builder.SetTrigger(new SystemTrigger(SystemTriggerType.NetworkStateChange, false));
            await BackgroundExecutionManager.RequestAccessAsync();
            builder.Register();
        }
    }
}
