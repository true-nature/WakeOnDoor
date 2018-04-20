using Prism.Unity.Windows;
using SerialMonitor;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.System.Profile;

namespace TweLiteMonitorOnPC
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
            this.Suspending += OnSuspending;
        }

        protected override async Task OnLaunchApplicationAsync(LaunchActivatedEventArgs args)
        {
            if (!"Windows.IoT".Equals(AnalyticsInfo.VersionInfo.DeviceFamily) && taskRegistration == null)
            {
                await StarTaskAsync();
            }
            this.NavigationService.Navigate("Main", null);
        }

        /// <summary>
        /// アプリケーションの実行が中断されたときに呼び出されます。
        /// アプリケーションが終了されるか、メモリの内容がそのままで再開されるかに
        /// かかわらず、アプリケーションの状態が保存されます。
        /// </summary>
        /// <param name="sender">中断要求の送信元。</param>
        /// <param name="e">中断要求の詳細。</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: アプリケーションの状態を保存してバックグラウンドの動作があれば停止します
            taskRegistration?.Unregister(true);
            taskRegistration = null;
            deferral.Complete();
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
            taskRegistration.Completed += async (sender, args) =>
            {
                if (IsSuspending) return;
                // restart when ExecutionTimeExceeded
                await trigger.RequestAsync();
            };
            var result = await trigger.RequestAsync();
            return result;
        }
    }
}
