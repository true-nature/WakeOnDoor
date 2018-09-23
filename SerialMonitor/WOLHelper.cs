using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage;

namespace SerialMonitor
{
    internal class WOLHelper
    {
        internal static async Task WakeUpAllAsync()
        {
            var settings = ApplicationData.Current.LocalSettings;
            var targetDic = SettingsEditor.ReadMacList(settings);
            foreach (var t in targetDic)
            {
                await WakeUpAsync(t.Value.Physical.Replace("-",""), t.Value.Address, t.Value.Port);
            }
        }

        internal static async Task<bool> WakeUpAsync(string physical, string address, string port)
        {
            var result = false;
            if (string.IsNullOrWhiteSpace(physical))
            {
                return false;
            }
            try
            {
                var parsed_physical = UInt64.TryParse(physical, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong mac);
                var host = new HostName(address);
                var parsed_port = Int32.TryParse(port, out int portNo);
                if (parsed_physical && parsed_port)
                {
                    await WakeUpAsync(mac, host, port);
                    result = true;
                }
            }
            catch (Exception)
            {

            }
            return result;
        }

        private static async Task WakeUpAsync(ulong mac, HostName hostname, string port)
        {
            byte[] buf = new byte[6*17];
            for (int k =0; k<6; k++)
            {
                buf[k] = 0xFF;
            }
            for (int i = 1; i < 17; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    buf[6 * i + j] = (byte)(0xff & (mac >> (8 * (5 - j))));
                }
            }
            using (var socket = new DatagramSocket())
            {
                using (Stream stream = (await socket.GetOutputStreamAsync(hostname, port)).AsStreamForWrite())
                {
                    await stream.WriteAsync(buf, 0, buf.Length);
                }
            }
        }
    }
}
