using System;
using System.Linq;
using System.Text;
using System.Threading;
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

        private static LogReceiveServer singleton;

        private SemaphoreSlim semaphore;
        private bool IsConnected;
        private DatagramSocket socket;
        private StringBuilder builder;

        public string Description { get { return SERVERHOST + ":" + PORT; } }

        private LogReceiveServer()
        {
            semaphore = new SemaphoreSlim(1, 1);
            builder = new StringBuilder();
        }

        public static LogReceiveServer GetInstance()
        {
            if (singleton == null)
            {
                var instance = new LogReceiveServer();
                singleton = instance;
            }
            return singleton;
        }

        public static async Task ShutdownAsync()
        {
            var instance = singleton;
            if (instance != null)
            {
                instance = null;
                await instance.DisconnectAsync();
            }
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
            socket.MessageReceived -= OnDatagramMessageReceived;
            socket?.Dispose();
            socket = null;
        }

        public async Task ConnectAsync()
        {
            await semaphore.WaitAsync();
            try
            {
                if (!IsConnected)
                {
                    var result = await OpenAsync();
                    IsConnected = result;
                    if (result)
                    {
#pragma warning disable CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
                        StartAsync();
#pragma warning restore CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task DisconnectAsync()
        {
            await semaphore.WaitAsync();
            try
            {
                if (IsConnected)
                {
                    Stop();
                    Close();
                    IsConnected = false;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task<bool> OpenAsync()
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

        private Task StartAsync()
        {
            if (socket != null)
            {
                socket.MessageReceived += OnDatagramMessageReceived;
            }
            return Task.CompletedTask;
        }

        private void Stop()
        {
            if (socket != null)
            {
                socket.MessageReceived -= OnDatagramMessageReceived;
            }
        }
    }
}
