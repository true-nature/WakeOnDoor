using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace SerialMonitor
{
    internal class SerialCommService : ISerialCommService, IDisposable
    {
        private const uint BUFFER_LENGTH = 256;
        private SerialDevice device;
        private DeviceInformation info;
        private CancellationTokenSource Source;

        private string portName;

        public event TypedEventHandler<ICommService, MessageEventArgs> Received;

        public string PortName
        {
            get { return this.portName; }
            set
            {
                this.portName = value;
            }
        }

        public DeviceInformation DeviceInfo
        {
            get
            {
                return info;
            }
        }

        public string Description { get { return DeviceInfo?.Id; } }

        public SerialCommService()
        {
            device = null;
            info = null;
        }

        public void Close()
        {
            if (device != null)
            {
                device.Dispose();
                device = null;
                info = null;
            }
            Source?.Dispose();
            Source = null;
        }

        public void Dispose()
        {
            device?.Dispose();
            Source?.Dispose();
        }

        public async Task<bool> OpenAsync()
        {
            var ds = SerialDevice.GetDeviceSelector();
            if (portName != null)
            {
                // Use PortName if specified
                ds = SerialDevice.GetDeviceSelector(portName);
            }
            var devices = await DeviceInformation.FindAllAsync(ds);
            // Search TWELITE
            foreach (var d in devices)
            {
                // "TWE-Lite-USB" or "TWE-Lite-R" is expected
                if (d.Name.StartsWith("TWE-Lite-"))
                {
                    // Use the first
                    device = await SerialDevice.FromIdAsync(d.Id);
                    if (device != null)
                    {
                        info = d;
                    }
                    break;
                }
            }
            if (device != null)
            {
                device.BaudRate = 115200;
                device.DataBits = 8;
                device.StopBits = SerialStopBitCount.One;
                device.Parity = SerialParity.None;
                device.Handshake = SerialHandshake.None;
                device.ReadTimeout = TimeSpan.FromMilliseconds(10);
                device.WriteTimeout = TimeSpan.FromMilliseconds(10);
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task StartAsync()
        {
            if (device == null) { return; }
            Source = new CancellationTokenSource();
            var Token = Source.Token;
            var sb = new StringBuilder();
            using (var reader = new DataReader(device.InputStream))
            {
                reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                while (true)
                {
                    var len = await reader.LoadAsync(BUFFER_LENGTH);
                    if (Token.IsCancellationRequested || device == null)
                    {
                        break;
                    }
                    if (len > 0)
                    {
                        var received = reader.ReadString(len);
                        foreach (var c in received)
                        {
                            if (c == '\r' || c == '\n')
                            {
                                if (sb.Length > 0)
                                {
                                    Received?.Invoke(this, new MessageEventArgs(sb.ToString()));
                                    sb.Clear();
                                }
                            }
                            else
                            {
                                sb.Append((char)c);
                            }
                        }
                    }
                }
            }
        }

        public void Stop()
        {
            Source?.Cancel();
        }
    }
}
