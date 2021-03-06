﻿using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using TweLiteMonitorOnPC.ViewModels;
using TweLiteMonitorOnPC.Views;
using Windows.ApplicationModel;
using Windows.UI.Xaml;

namespace TweLiteMonitorOnPC
{
    /// <summary>
    /// 既定の Application クラスを補完するアプリケーション固有の動作を提供します。
    /// </summary>
    sealed partial class App : PrismApplication
    {
        private FrameworkElement MainPage { get; set; }

        /// <summary>
        /// 単一アプリケーション オブジェクトを初期化します。これは、実行される作成したコードの
        ///最初の行であるため、main() または WinMain() と論理的に等価です。
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Resuming += App_Resuming;
            this.Suspending += OnSuspending;
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }

        protected override UIElement CreateShell()
        {
            MainPage = Container.Resolve<MainPage>();
            return MainPage;
        }

        private void App_Resuming(object sender, object e)
        {
            var vm = MainPage.DataContext as MainPageViewModel;
            vm?.OnLoad();
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
            // アプリケーションの状態を保存してバックグラウンドの動作があれば停止します
            var vm = MainPage.DataContext as MainPageViewModel;
            vm?.OnClosing();
            deferral.Complete();
        }
    }
}
