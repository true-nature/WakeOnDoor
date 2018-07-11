using Prism.Unity.Windows;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using WakeOnDoor.Models;
using WakeOnDoor.Views;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources.Core;
using Windows.Globalization;
using Windows.Storage;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
            ClearTempSettings();
            EditorModel = TargetEditorModel.GetInstance();
            EditorModel.PropertyChanged += OnModelPropertyChanged;
            LogModel = PacketLogModel.GetInstance();
            await LogModel.InitializeAsync();
            this.NavigationService.Navigate("Navigation", null);
        }

        private async void OnModelPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            switch (args.PropertyName)
            {
                case nameof(EditorModel.Language):
                    await  Shell.Dispatcher.TryRunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        // FIDME not navigated
                        ApplicationLanguages.PrimaryLanguageOverride = EditorModel.Language;
                        var rootFrame = Window.Current.Content as Frame;
                        rootFrame.CacheSize = 0;
                        ResourceContext.GetForCurrentView().Reset();
                        ResourceContext.GetForViewIndependentUse().Reset();
                        rootFrame.Navigate(typeof(NavigationPage));
                    });
                    break;
                default:
                    break;
            }
        }

        private static void ClearTempSettings()
        {
            var settings = ApplicationData.Current.LocalSettings;
            foreach (var key in settings.Values.Keys)
            {
                if (key.StartsWith("Temp."))
                {
                    settings.Values.Remove(key);
                }
            }
        }

        private PacketLogModel LogModel;
        private TargetEditorModel EditorModel;
        private static bool deviceFamily = ("Windows.IoT".Equals(AnalyticsInfo.VersionInfo.DeviceFamily));
        public static bool IsIoTDeviceFamily { get { return deviceFamily; } }
    }
}
