using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;

namespace TweLiteMonitor
{
    internal class LogWriter
    {
        private const string PORT = "9514";
        //private const string LOCALHOST = "127.0.0.1";
        private const string LOCALHOST = "::1";
        private Stream stream;
        private TextWriter writer;

        private DatagramSocket socket;
        public async Task<bool> OpenAsync()
        {
            bool result = false;
            try
            {
                var hostname = new HostName("::1");
                socket?.Dispose();
                socket = new DatagramSocket();
                stream = (await socket.GetOutputStreamAsync(hostname, PORT)).AsStreamForWrite();
                writer = new StreamWriter(stream);
                result = true;
            } catch (Exception)
            {

            }
            return result;
        }

        public async Task WriteAsync(string msg)
        {
            await writer?.WriteLineAsync(msg);
            await writer?.FlushAsync();
        }

        public void Close()
        {
            writer?.Dispose();
            stream?.Dispose();
            socket?.Dispose();
            writer = null;
            stream = null;
            socket = null;
        }
    }
}
