using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace WakeOnDoor.Services
{
    public interface ISerialCommService : ICommService
    {

        string PortName { get; set; }
        DeviceInformation DeviceInfo { get; }
    }
}
