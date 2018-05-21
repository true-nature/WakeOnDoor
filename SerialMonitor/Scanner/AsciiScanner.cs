using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SerialMonitor.Scanner
{
    /// ascii style
    /// ADXL345
    /// :80000000 90 0071 810F1D2F 00 35 A2 0480 027C 08 FFFE FF9C FFDB 50
    /// BUTTON
    /// :80000000 42 9048 810118CA 02 FE B7 03C2 0239 01 01 01 48
    internal class AsciiScanner : IMessageScanner
    {
        private static readonly Regex ADXL345Regex = new Regex(@"^:(?<rptr>[\dA-F]{8})(?<lqi>[\dA-F]{2})(?<ct>[\dA-F]{4})(?<sid>[\dA-F]{8})(?<id>[\dA-F]{2})(?<pkt>[\dA-F]{2})(?<bt>[\dA-F]{2})(?<adc1>[\dA-F]{4})(?<adc2>[\dA-F]{4})(?<mode>[\dA-F]{2})(?<x>[\dA-F]{4})(?<y>[\dA-F]{4})(?<z>[\dA-F]{4})(?<CHK>[\dA-F]{2})$");
        private static readonly Regex ButtonRegex = new Regex(@"^:(?<rptr>[\dA-F]{8})(?<lqi>[\dA-F]{2})(?<ct>[\dA-F]{4})(?<sid>[\dA-F]{8})(?<id>[\dA-F]{2})(?<pkt>[\dA-F]{2})(?<bt>[\dA-F]{2})(?<adc1>[\dA-F]{4})(?<adc2>[\dA-F]{4})(?<mode>[\dA-F]{2})(?<din>[\dA-F]{2})(?<dout>[\dA-F]{2})?(?<CHK>[\dA-F]{2})$");
        private static readonly Regex[] regexs = { ADXL345Regex, ButtonRegex };

        public AsciiScanner()
        {
        }

        public TagInfo Scan(string msg)
        {
            TagInfo info = new TagInfo() { valid = false };
            if (msg.StartsWith(":"))
            {
                foreach (var r in regexs)
                {
                    Match m = r.Match(msg);
                    if (m.Success)
                    {
                        CopyInfo(info, m.Groups, r.GetGroupNames());
                        if(info.valid && !(info.pkt == 0xFE && ((info.din ^ info.mode) & 1) == 0))
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
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out info.lqi);
                            break;
                        case nameof(TagInfo.ct):
                            valid &= ushort.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out info.ct);
                            break;
                        case nameof(TagInfo.sid):
                            valid &= uint.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out info.sid);
                            if (valid) { info.serial = (info.sid & 0x7FFFFFFF); }
                            break;
                        case nameof(TagInfo.serial):
                            valid &= uint.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out info.serial);
                            if (valid) { info.sid = (info.serial | 0x80000000); }
                            break;
                        case nameof(TagInfo.id):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out info.id);
                            break;
                        case nameof(TagInfo.pkt):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out info.pkt);
                            break;
                        case nameof(TagInfo.bt):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out info.bt);
                            if (valid)
                            {
                                info.batt = TagInfo.DecodeVolt(info.bt);
                            }
                            break;
                        case nameof(TagInfo.adc1):
                            valid &= ushort.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out info.adc1);
                            break;
                        case nameof(TagInfo.adc2):
                            valid &= ushort.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out info.adc2);
                            break;
                        case nameof(TagInfo.mode):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out info.mode);
                            break;
                        case nameof(TagInfo.din):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out info.din);
                            break;
                        case nameof(TagInfo.dout):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out info.dout);
                            break;
                        case nameof(TagInfo.x):
                            valid &= short.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out info.x);
                            break;
                        case nameof(TagInfo.y):
                            valid &= short.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out info.y);
                            break;
                        case nameof(TagInfo.z):
                            valid &= short.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out info.z);
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
