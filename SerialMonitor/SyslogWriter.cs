﻿using System;
using System.Globalization;
using System.Threading.Tasks;

namespace SerialMonitor
{
    /// <summary>
    /// BSD syslog writer
    /// </summary>
    internal class SyslogWriter : IDisposable, ISyslogWriter
    {
        private const string MYHOST = "localhost";

        private LogWriter writer;

        private int fac_code;
        public string Tag { get; private set; }

        public SyslogWriter(Facility fac, string tag)
        {
            this.fac_code = ((int) fac)<<3;
            this.Tag = tag;
        }

        public async Task<bool> OpenAsync()
        {
            writer = await LogWriter.GetInstanceAsync();
            return (writer != null);
        }

        private async Task WriteSyslog(Priority pri, string msg)
        {
            var now = DateTime.Now;
            var logmsg = string.Format(CultureInfo.InvariantCulture, "<{0}>{1:MMM dd HH:mm:ss} {2} [{3}] {4}", ((int)pri + fac_code), now, MYHOST, Tag, msg);
            await writer?.WriteAsync(logmsg);
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

        public void Dispose()
        {
            writer = null;
        }
    }
}
