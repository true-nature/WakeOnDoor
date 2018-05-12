using Prism.Unity.Windows;
using System;
using System.Threading.Tasks;
using WakeOnDoor.Services;
using Windows.ApplicationModel.Activation;

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
            var commService = LogReceiveServer.GetInstance();
            await commService.ConnectAsync();
            this.NavigationService.Navigate("Navigation", null);
        }
    }
}
