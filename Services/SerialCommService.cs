using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace WakeOnDoor.Services
{
    public class SerialCommService : ISerialCommService
    {
        private SerialDevice device;
        private DeviceInformation info;

        private string portName;
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
                device.Handshake = SerialHandshake.RequestToSend;
                device.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                device.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<string> ReadAsync()
        {
            const uint BUFFER_LENGTH = 1;
            if (device != null)
            {
                var canceslsource = new CancellationTokenSource();
                var sb = new StringBuilder();
                var received = new byte[BUFFER_LENGTH];
                var sem = new SemaphoreSlim(initialCount: 1);
                var reader = new DataReader(device.InputStream);
                reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                while (true)
                {
                    var len = await reader.LoadAsync(BUFFER_LENGTH);
                    if (canceslsource.Token.IsCancellationRequested || device == null)
                    {
                        break;
                    }
                    if (len > 0)
                    {
                        reader.ReadBytes(received);
                        // TODO: Implement here
                    }
                }
            }
            return null;
        }
    }
}
