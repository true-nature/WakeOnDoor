using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;

namespace SerialMonitor
{
    internal class LogWriter: IDisposable, ISyslog
    {
        private const string SERVERHOST = "::1";
        private const string MYHOST = "localhost";
        private Stream stream;
        private TextWriter writer;
        private DatagramSocket socket;

        private int fac_code;
        public string Tag { get; private set; }

	public LogWriter(Facility fac, string tag)
        {
            this.fac_code = ((int)fac)<<3;
            this.Tag = tag;
        }

        public async Task<bool> OpenAsync()
        {
            bool result = false;
            try
            {
                var hostname = new HostName(SERVERHOST);
                var port = ((ushort)Port.internal_syslog).ToString();
                socket?.Dispose();
                socket = new DatagramSocket();
                stream = (await socket.GetOutputStreamAsync(hostname, port)).AsStreamForWrite();
                writer = new StreamWriter(stream);
                result = true;
            }
            catch (Exception)
            {
                Dispose();
            }
            return result;
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

        private async Task WriteAsync(string msg)
        {
            await writer?.WriteLineAsync(msg);
            await writer?.FlushAsync();
        }

        private async Task WriteSyslog(Priority pri, string msg)
        {
            var now = DateTime.Now;
            var logmsg = string.Format(CultureInfo.InvariantCulture, "<{0}>{1:MMM dd HH:mm:ss} {2} [{3}] {4}", ((int)pri + fac_code), now, MYHOST, Tag, msg);
            await WriteAsync(logmsg);
        }

        public async Task Notice(string msg)
        {
            await WriteSyslog(Priority.notice, msg);
        }

        public async Task Emerg(string msg)
        {
            await WriteSyslog(Priority.emerg, msg);
        }

        public async Task Alert(string msg)
        {
            await WriteSyslog(Priority.alert, msg);
        }

        public async Task Crit(string msg)
        {
            await WriteSyslog(Priority.crit, msg);
        }

        public async Task Err(string msg)
        {
            await WriteSyslog(Priority.err, msg);
        }

        public async Task Warning(string msg)
        {
            await WriteSyslog(Priority.warning, msg);
        }

        public async Task Info(string msg)
        {
            await WriteSyslog(Priority.info, msg);
        }

        public async Task Debug(string msg)
        {
            await WriteSyslog(Priority.debug, msg);
        }
    }
}
