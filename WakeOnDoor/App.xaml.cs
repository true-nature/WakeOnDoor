using Prism.Unity.Windows;
using SerialMonitor;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.System.Profile;

namespace WakeOnDoor
{
    /// <summary>
    /// 既定の Application クラスを補完するアプリケーション固有の動作を提供します。
    /// </summary>
    sealed partial class App : PrismUnityApplication
    {
        /// <summary>
        /// 単一アプリケーション オブジェクトを初期化します。これは、実行される作成したコードの
        ///最初の行であるため、main() または WinMain() と論理的に等価です。
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        protected override async Task OnLaunchApplicationAsync(LaunchActivatedEventArgs args)
        {
            if (!"Windows.IoT".Equals(AnalyticsInfo.VersionInfo.DeviceFamily) && taskRegistration == null)
            {
                await StarTaskAsync();
            }
            this.NavigationService.Navigate("Main", null);
            //return Task.CompletedTask;
        }

        private BackgroundTaskRegistration taskRegistration;
        private async Task<ApplicationTriggerResult> StarTaskAsync()
        {
            const string TaskName = "TweLiteMonitor-uwp";
            foreach (var t in BackgroundTaskRegistration.AllTasks)
            {
                if (t.Value.Name == TaskName)
                {
                    return ApplicationTriggerResult.CurrentlyRunning;
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
            var result = await trigger.RequestAsync();
            return result;
        }
        
        protected override async Task OnSuspendingApplicationAsync()
        {
            taskRegistration?.Unregister(true);
            taskRegistration = null;
            await base.OnSuspendingApplicationAsync();
        }
    }
}
