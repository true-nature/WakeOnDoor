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
        private const string ALLNODE = "FF02::1";   // Link-Local Scope Multicast Addresses / All Nodes Address
        private const string PORT = "7";

        internal static async Task WakeUpAllAsync()
        {
            var settings = ApplicationData.Current.LocalSettings;
            var targetDic = SettingsEditor.ReadMacList(settings);
            foreach (var t in targetDic)
            {
                await WakeUpAsync(t.Value.Physical.Replace("-",""));
            }
        }

        private static async Task WakeUpAsync(string physical)
        {
            if (string.IsNullOrWhiteSpace(physical))
            {
                return;
            }
            try
            {
                var parsed = UInt64.TryParse(physical, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong mac);
                if (parsed)
                {
                    await WakeUpAsync(mac);
                }
            } catch (Exception)
            {

            }
        }

        private static async Task WakeUpAsync(ulong mac)
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
            var hostname = new HostName(ALLNODE);
            using (var socket = new DatagramSocket())
            {
                using (Stream stream = (await socket.GetOutputStreamAsync(hostname, PORT)).AsStreamForWrite())
                {
                    await stream.WriteAsync(buf, 0, buf.Length);
                }
            }
        }
    }
}
