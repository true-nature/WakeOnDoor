using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;

namespace WakeOnDoor.Services
{
    internal class LogReceiveServer : ICommService
    {
        private const string PORT = "9514";
        //private const string SERVERHOST = "127.0.0.1";
        private const string SERVERHOST = "::1";

        private DatagramSocket socket;
        StringBuilder builder;

        public string Description { get { return SERVERHOST + ":" + PORT; } }

        public LogReceiveServer()
        {
            builder = new StringBuilder();
        }

        public event TypedEventHandler<ICommService, MessageEventArgs> Received;

        public void OnDatagramMessageReceived(DatagramSocket socket, DatagramSocketMessageReceivedEventArgs args)
        {
            try
            {
                var reader = args.GetDataReader();
                var length = reader.UnconsumedBufferLength;
                var received = reader.ReadString(length);
                foreach (var ch in received)
                {
                    if (ch == '\r' || ch == '\n')
                    {
                        if (builder.Length > 0)
                        {
                            Received?.Invoke(this, new MessageEventArgs(builder.ToString()));
                            builder.Clear();
                        }
                    }
                    else
                    {
                        builder.Append(ch);
                    }
                }
            } catch (Exception)
            {

            }
        }

        public void Close()
        {
            socket?.Dispose();
            socket.MessageReceived -= OnDatagramMessageReceived;
            socket = null;
        }

        public async Task<bool> OpenAsync()
        {
            var result = false;
            try
            {
                var hostname = new HostName(SERVERHOST);
                socket?.Dispose();
                socket = new DatagramSocket();
                socket.Control.InboundBufferSizeInBytes = 256;
                //await socket.BindEndpointAsync(hostname, PORT);
                await socket.BindServiceNameAsync(PORT);
                result = true;
            } catch (Exception)
            {
                Close();
            }
            return result;
        }

        public Task StartAsync()
        {
            if (socket != null)
            {
                socket.MessageReceived += OnDatagramMessageReceived;
            }
            return Task.CompletedTask;
        }

        public void Stop()
        {
            if (socket != null)
            {
                socket.MessageReceived -= OnDatagramMessageReceived;
            }
        }
    }
}
