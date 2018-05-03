using SerialMonitor;
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
        public Facility Facility { get; }
        public Priority Priority { get; }
        public DateTime Timestamp { get; }
        public string Tag { get; }
        public string Message { get; }
        public MessageEventArgs(string msg)
        {
            Message = msg;
            Timestamp = DateTime.Now;
        }

        public MessageEventArgs(Facility fac, Priority pri, DateTime ts, string tag, string msg)
        {
            Facility = fac;
            Priority = pri;
            Timestamp = ts;
            Tag = tag;
            Message = msg;
        }
    }

    public interface ICommService
    {
        event TypedEventHandler<ICommService,MessageEventArgs> Received;

        Task ConnectAsync();

        Task DisconnectAsync();
    }
}
