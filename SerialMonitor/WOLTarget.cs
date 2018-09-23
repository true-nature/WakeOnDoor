using System.Runtime.Serialization;

namespace SerialMonitor
{
    [DataContract]
    public sealed class WOLTarget
    {
        [DataMember(Name = "physical")]
        public string Physical { get; set; }
        [DataMember(Name = "address")]
        public string Address { get; set; }
        [DataMember(Name = "port")]
        public string Port { get; set; }
        [DataMember(Name ="comment")]
        public string Comment { get; set; }

        private const string DEFAULT_ADDRESS = "FF02::1";   // Link-Local Scope Multicast Addresses / All Nodes Address

        public WOLTarget()
        {
            Physical = "";
            Address = DEFAULT_ADDRESS;
            Port = "9";
            Comment = "";
        }
    }
}
