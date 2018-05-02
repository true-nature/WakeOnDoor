using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace WakeOnDoor.Services
{
    public class MessageEventArgs : EventArgs
    {
        public string Message { get; }
        public MessageEventArgs(string msg)
        {
            Message = msg;
        }
    }

    public interface ICommService
    {
        event TypedEventHandler<ICommService,MessageEventArgs> Received;

        string Description { get; }

        Task ConnectAsync();

        Task DisconnectAsync();
    }
}
