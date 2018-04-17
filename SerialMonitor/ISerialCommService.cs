using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace SerialMonitor
{
    internal interface ISerialCommService : ICommService
    {

        string PortName { get; set; }
        DeviceInformation DeviceInfo { get; }
    }
}
