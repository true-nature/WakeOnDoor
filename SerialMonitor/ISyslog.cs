using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialMonitor
{
    internal interface ISyslog
    {
        Task Emerg(string msg);
        Task Alert(string msg);
        Task Crit(string msg);
        Task Err(string msg);
        Task Warning(string msg);
        Task Notice(string msg);
        Task Info(string msg);
        Task Debug(string msg);
    }

    public enum Port
    {
        internal_syslog = 9514,
    }

    public enum Priority
    {
        emerg = 0,
        alert,
        crit,
        err,
        warning,
        notice,
        info,
        debug,
    }
    public enum Facility
    {
        kern = 0,
        user,
        mail,
        daemon,
        auth,
        syslog,
        lpr,
        news,
        uucp,
        cron,
        autpriv,
        ftp,
        local0 = 16,
        local1,
        local2,
        local3,
        local4,
        local5,
        local6,
        local7,
    }

    interface ISyslogWriter : ISyslog
    {
        Task<bool> OpenAsync();
    }
}
