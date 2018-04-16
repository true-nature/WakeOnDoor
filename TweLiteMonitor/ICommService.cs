using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace TweLiteMonitor
{
    internal class MessageEventArgs : EventArgs
    {
        public string Message { get; }
        public MessageEventArgs(string msg)
        {
            Message = msg;
        }
    }

    internal interface ICommService
    {
        event TypedEventHandler<ICommService,MessageEventArgs> Received;

        string Description { get; }

        /// <summary>
        /// Open communication channel.
        /// </summary>
        /// <returns>true if success</returns>
        Task<bool> OpenAsync();

        /// <summary>
        /// Infinite read.
        /// </summary>
        /// <returns></returns>
        Task StartAsync();

        /// <summary>
        /// Stop read loop.
        /// </summary>
        void Stop();

        /// <summary>
        /// Close communication channel.
        /// </summary>
        void Close();

    }
}
