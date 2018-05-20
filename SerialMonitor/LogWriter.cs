using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;

namespace SerialMonitor
{
    internal class LogWriter : IDisposable, ILogWriter
    {
        private const string SERVERHOST = "::1";
        private Stream stream;
        private TextWriter writer;

        private DatagramSocket socket;

        private static LogWriter instance;
        private static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        private LogWriter()
        {
        }

        public static async Task<LogWriter> GetInstanceAsync()
        {
            await semaphore.WaitAsync();
            try
            {
                if (instance == null)
                {
                    var writer = new LogWriter();
                    var result = await writer.OpenAsync();
                    if (result)
                    {
                        instance = writer;
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
            return instance;
        }

        private async Task<bool> OpenAsync()
        {
            bool result = false;
            try
            {
                if (writer == null)
                {
                    var hostname = new HostName(SERVERHOST);
                    socket?.Dispose();
                    socket = new DatagramSocket();
                    var port = (ushort)Port.internal_syslog;
                    stream = (await socket.GetOutputStreamAsync(hostname, port.ToString())).AsStreamForWrite();
                    writer = new StreamWriter(stream);
                    result = true;
                }
            }
            catch (Exception)
            {
                Dispose();
            }
            return result;
        }

        public async Task WriteAsync(string msg)
        {
            await semaphore.WaitAsync();
            try
            {
                await writer?.WriteLineAsync(msg);
                await writer?.FlushAsync();
            }
            finally
            {
                semaphore.Release();
            }
        }

        public void Dispose()
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
