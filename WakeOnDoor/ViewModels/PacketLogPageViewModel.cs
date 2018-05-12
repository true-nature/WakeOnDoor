using Prism.Commands;
using Prism.Windows.Mvvm;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Input;
using WakeOnDoor.Services;
using Windows.UI.Core;

namespace WakeOnDoor.ViewModels
{
    public class PacketLogPageViewModel : ViewModelBase
    {
        private const int LOG_CAPACITY = 50;

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

        public PacketLogPageViewModel()
        {
            IsConnected = false;
            textLog = new List<string>();

            commService = LogReceiveServer.GetInstance();
            commService.Received += this.OnReceived;

            this.ClearLogCommand = new DelegateCommand(() =>
            {
                textLog.Clear();
                RaisePropertyChanged(nameof(TextLog));
            });

#pragma warning disable CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
            commService.ConnectAsync();
#pragma warning restore CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
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
                textLog.Add(string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", args.Timestamp, args.Priority, args.Tag, args.Message));
                RaisePropertyChanged(nameof(TextLog));
            });
#pragma warning restore CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
        }
    }
}
