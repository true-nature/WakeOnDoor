﻿using System.Threading.Tasks;

namespace SerialMonitor
{
    interface ILogWriter
    {
        Task WriteAsync(string msg);
    }
}
