using System.Threading.Tasks;

namespace SerialMonitor.Scanner
{
    internal interface IMessageScanner
    {
        /// <summary>
        /// Analyze UART output of App_Tag on TWELITE.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>true if message was handled.</returns>
        Task<TagInfo> ScanAsync(string msg);
    }
}
