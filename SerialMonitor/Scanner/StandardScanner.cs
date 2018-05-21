using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SerialMonitor.Scanner
{
    /// ADXL345 mode
    /// ::rc=80000000:lq=84:ct=0069:ed=810F1D2F:id=0:ba=2760:a1=0984:a2=0571:x=0000:y=-097:z=-045
    /// BUTTON mode (Can't use because DI bitmap is absent)
    /// ::rc=80000000:lq=123:ct=12E4:ed=81011157:id=4:ba=2880:bt=0001
    internal class StandardScanner : IMessageScanner
    {
        private static readonly Regex ADXL345Regex = new Regex(@"::rc=(?<relay>[\dA-F]{8}):lq=(?<lqi>\d+):ct=(?<ct>[\dA-F]{4}):ed=(?<sid>[\dA-F]{8}):id=(?<id>[\dA-F]+):ba=(?<batt>\d+):a1=(?<adc1>\d+):a2=(?<adc2>\d+):x=(?<x>-?\d+):y=(?<y>-?\d+):z=(?<z>-?\d+)$");
        //private static readonly Regex ButtonRegex = new Regex(@"::rc=(?<relay>[\dA-F]{8}):lq=(?<lqi>\d+):ct=(?<ct>[\dA-F]{4}):ed=(?<sid>[\dA-F]{8}):id=(?<id>[\dA-F]+):ba=(?<batt>\d+):bt=(?<dout>\d+)$");
        private static readonly Regex[] regexs = { ADXL345Regex };

        public StandardScanner()
        {
        }

        public TagInfo Scan(string msg)
        {
            TagInfo info = new TagInfo() { valid = false };
            if (msg.StartsWith("::"))
            {
                foreach (var r in regexs)
                {
                    Match m = r.Match(msg);
                    if (m.Success)
                    {
                        CopyInfo(info, m.Groups, r.GetGroupNames());
                        if (info.valid && !(info.pkt == 0xFE && ((info.din ^ info.mode) & 1) == 0))
                        {
                            info.wolTrigger = true;
                            break;
                        }
                    }
                }
            }
            return info;
        }

        private static void CopyInfo(TagInfo info, GroupCollection groups, string[] names)
        {
            var valid = true;
            foreach (var name in names)
            {
                try
                {
                    switch (name)
                    {
                        case nameof(TagInfo.rptr):
                            valid &= uint.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out info.rptr);
                            break;
                        case nameof(TagInfo.lqi):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out info.lqi);
                            break;
                        case nameof(TagInfo.ct):
                            valid &= ushort.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out info.ct);
                            break;
                        case nameof(TagInfo.sid):
                            valid &= uint.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out info.sid);
                            if (valid) { info.serial = (info.sid & 0x7FFFFFFF); }
                            break;
                        case nameof(TagInfo.id):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out info.id);
                            break;
                        case nameof(TagInfo.batt):
                            valid &= ushort.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out info.batt);
                            break;
                        case nameof(TagInfo.adc1):
                            valid &= ushort.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out info.adc1);
                            break;
                        case nameof(TagInfo.adc2):
                            valid &= ushort.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out info.adc2);
                            break;
                        case nameof(TagInfo.mode):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out info.mode);
                            break;
                        case nameof(TagInfo.din):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out info.din);
                            break;
                        case nameof(TagInfo.dout):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out info.dout);
                            info.pkt = 0xfe;
                            break;
                        case nameof(TagInfo.x):
                            valid &= short.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out info.x);
                            info.pkt = 0x35;
                            break;
                        case nameof(TagInfo.y):
                            valid &= short.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out info.y);
                            break;
                        case nameof(TagInfo.z):
                            valid &= short.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out info.z);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception)
                {
                    valid = false;
                }
                if (!valid) { break; }
            }
            info.valid = valid;
        }
    }
}
