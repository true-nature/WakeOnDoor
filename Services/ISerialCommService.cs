using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace WakeOnDoor.Services
{
    public interface ISerialCommService
    {

        string PortName { get; set; }
        DeviceInformation DeviceInfo { get; }
        /// <summary>
        /// Open serial port.
        /// </summary>
        /// <param name="port">port name</param>
        /// <returns>true if success</returns>
        Task<bool> OpenAsync();

        /// <summary>
        /// Close serial port.
        /// </summary>
        void Close();


    }
}
