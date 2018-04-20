using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Diagnostics;

namespace SerialMonitor
{
    [DataContract]
    public sealed class WOLTarget
    {
        [DataMember(Name = "physical")]
        public string Physical { get; set; }
        [DataMember(Name ="comment")]
        public string Comment { get; set; }

        public WOLTarget()
        {
            Physical = "";
            Comment = "";
        }
    }
}
