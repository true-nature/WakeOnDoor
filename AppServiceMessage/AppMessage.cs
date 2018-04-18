using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppServiceMessage
{
    public static class AppMessage
    {
        public const string KEY_COMMAND = "Command";
        public const string KEY_MACLIST = "MacList";
        public const string KEY_MAC_ADDRESS = "MacAddress";
        public const string KEY_RESULT = "Result";
        public const string CMD_ADD = "Add";
        public const string CMD_REMOVE = "Remove";
        public const string CMD_GET = "Get";
    }

    class AppRequest
    {
        public string command { get; set; }
        public string mac { get; set; }
    }

    class AppResponse
    {
        public string status { get; set; }
        public string[] maclist { get; set; }
    }
}
