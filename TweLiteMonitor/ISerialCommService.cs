using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace TweLiteMonitor
{
    internal interface ISerialCommService : ICommService
    {

        string PortName { get; set; }
        DeviceInformation DeviceInfo { get; }
    }
}
