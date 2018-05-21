using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WakeOnDoor.Services;

namespace WakeOnDoor.Models
{
    internal class PacketLogModel : BindableBase, IDisposable
    {
        private const int LOG_CAPACITY = 50;

        private static PacketLogModel instance;

        private ICommService commService;

        private List<string> textLog;
        public List<string> LogList
        {
            get { return textLog; }
        }

        private PacketLogModel()
        {
            textLog = new List<string>();
            commService = LogReceiveServer.GetInstance();
            commService.Received += this.OnReceived;

        }

        public static PacketLogModel GetInstance()
        {
            if (instance == null)
            {
                instance = new PacketLogModel();
            }
            return instance;
        }

        public async Task InitializeAsync()
        {
            await commService.ConnectAsync();
        }

        public async void Dispose()
        {
            await commService.DisconnectAsync();
        }

        private void OnReceived(ICommService sender, MessageEventArgs args)
        {
            while (textLog.Count > LOG_CAPACITY)
            {
                textLog.RemoveAt(0);
            }
            textLog.Add(string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", args.Timestamp, args.Priority, args.Tag, args.Message));
            RaisePropertyChanged(nameof(LogList));
        }
    }
}
