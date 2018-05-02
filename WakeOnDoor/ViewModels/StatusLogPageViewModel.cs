using Prism.Commands;
using Prism.Windows.Mvvm;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WakeOnDoor.Services;
using Windows.UI.Core;

namespace WakeOnDoor.ViewModels
{
    public class StatusLogPageViewModel : ViewModelBase
    {
        private const int LOG_CAPACITY = 50;
        private SemaphoreSlim semaphore;

        public ICommand ClearLogCommand { get; }

        private CoreDispatcher dispatcher;
        public CoreDispatcher Dispatcher
        {
            get { return dispatcher; }
            set
            {
                dispatcher = value;
            }
        }
        private ICommService commService;

        private bool isConnected;
        public bool IsConnected
        {
            get { return this.isConnected; }
            set
            {
                SetProperty(ref isConnected, value);
            }
        }

        private List<string> textLog;
        public string TextLog
        {
            get { return string.Join("\n", textLog); }
        }

        public StatusLogPageViewModel()
        {
            IsConnected = false;
            textLog = new List<string>();

            semaphore = new SemaphoreSlim(1, 1);
            commService = new LogReceiveServer();
            commService.Received += this.OnReceived;

            this.ClearLogCommand = new DelegateCommand(() =>
            {
                textLog.Clear();
                RaisePropertyChanged(nameof(TextLog));
            });

#pragma warning disable CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
            ConnectAsync();
#pragma warning restore CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
        }
        private async Task ConnectAsync()
        {
            await semaphore.WaitAsync();
            try
            {
                if (!IsConnected)
                {
                    var result = await commService.OpenAsync();
                    IsConnected = result;
                    if (result)
                    {
#pragma warning disable CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
                        commService.StartAsync();
#pragma warning restore CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private void OnReceived(ICommService sender, MessageEventArgs args)
        {
#pragma warning disable CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
            Dispatcher.TryRunAsync(CoreDispatcherPriority.Normal, () =>
            {
                while (textLog.Count > LOG_CAPACITY)
                {
                    textLog.RemoveAt(0);
                }
                textLog.Add(args.Message);
                RaisePropertyChanged(nameof(TextLog));
            });
#pragma warning restore CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
        }

        private async Task DisconnectAsync()
        {
            await semaphore.WaitAsync();
            try
            {
                if (IsConnected)
                {
                    commService.Stop();
                    commService.Close();
                    IsConnected = false;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
