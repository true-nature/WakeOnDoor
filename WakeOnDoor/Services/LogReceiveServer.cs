using SerialMonitor;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;

namespace WakeOnDoor.Services
{
    public class LogReceiveServer : ICommService
    {
        private const string SERVERHOST = "::1";
        private static readonly Regex SyslogRegex = new Regex(@"^<(?<pri>\d{1,3})>(?<month>[^ ]{3}) (?<day>\d\d) (?<hour>\d\d):(?<min>\d\d):(?<sec>\d\d) (?<host>[^ ]+)( (?<tag>\[[^ ]+\]))? (?<msg>.*)$");

        private readonly SemaphoreSlim semaphore;
        private bool IsConnected;
        private DatagramSocket socket;
        private readonly StringBuilder builder;

        public LogReceiveServer()
        {
            semaphore = new SemaphoreSlim(1, 1);
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
                            var logline = builder.ToString();
                            var m = SyslogRegex.Match(logline);
                            if (m.Success)
                            {
                                if (ushort.TryParse(m.Groups["pri"].Value, out ushort pr))
                                {
                                    var f = (Facility)(pr >> 3);
                                    var p = (Priority)(pr & 7);
                                    var tag = m.Groups["tag"].Value as string;
                                    Received?.Invoke(this, new MessageEventArgs(f, p, DateTime.Now, tag, m.Groups["msg"].Value));
                                }
                                else
                                {
                                    Received?.Invoke(this, new MessageEventArgs(logline));
                                }
                            }
                            else
                            {
                                Received?.Invoke(this, new MessageEventArgs(logline));
                            }
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
            if (socket != null)
            {
                socket.MessageReceived -= OnDatagramMessageReceived;
                socket.Dispose();
                socket = null;
            }
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
                        Start();
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
                var port = ((ushort)Port.internal_syslog).ToString();
                socket?.Dispose();
                socket = new DatagramSocket();
                socket.Control.InboundBufferSizeInBytes = 256;
                await socket.BindServiceNameAsync(port);
                result = true;
            } catch (Exception)
            {
                Close();
            }
            return result;
        }

        private void Start()
        {
            if (socket != null)
            {
                socket.MessageReceived += OnDatagramMessageReceived;
            }
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
