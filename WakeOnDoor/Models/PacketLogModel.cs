using Prism.Mvvm;
using SerialMonitor.Scanner;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using WakeOnDoor.Services;

namespace WakeOnDoor.Models
{
    public class PacketLogModel : BindableBase, IDisposable
    {
        private const int LOG_CAPACITY = 1000;

        private readonly ICommService commService;

        private readonly List<string> textLog;
        public List<string> LogList
        {
            get { return textLog; }
        }

        private TagInfo tagInfo;
        public TagInfo Tag
        {
            get { return tagInfo; }
            set
            {
                SetProperty(ref tagInfo, value);
            }
        }

        public PacketLogModel(ICommService logReceiveServer)
        {
            textLog = new List<string>();
            tagInfo = new TagInfo();
            commService = logReceiveServer;
            commService.Received += this.OnReceived;

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
            TagInfo tagValue = TagInfo.FromString(args.Message);
            if (tagValue.Valid)
            {
                Tag = tagValue;
            }
            while (textLog.Count > LOG_CAPACITY)
            {
                textLog.RemoveAt(0);
            }
            textLog.Add(string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", args.Timestamp, args.Priority, args.Tag, args.Message));
            RaisePropertyChanged(nameof(LogList));
        }
    }
}
