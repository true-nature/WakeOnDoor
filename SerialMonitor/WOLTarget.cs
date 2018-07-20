using System.Runtime.Serialization;

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
