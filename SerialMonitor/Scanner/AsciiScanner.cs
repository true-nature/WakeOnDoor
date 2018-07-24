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
        private static readonly Regex ADXL345Regex = new Regex(@"^:(?<Rptr>[\dA-F]{8})(?<Lqi>[\dA-F]{2})(?<Ct>[\dA-F]{4})(?<Sid>[\dA-F]{8})(?<Id>[\dA-F]{2})(?<Pkt>[\dA-F]{2})(?<Bt>[\dA-F]{2})(?<Adc1>[\dA-F]{4})(?<Adc2>[\dA-F]{4})(?<Mode>[\dA-F]{2})(?<X>[\dA-F]{4})(?<Y>[\dA-F]{4})(?<Z>[\dA-F]{4})(?<CHK>[\dA-F]{2})$");
        private static readonly Regex ButtonRegex = new Regex(@"^:(?<Rptr>[\dA-F]{8})(?<Lqi>[\dA-F]{2})(?<Ct>[\dA-F]{4})(?<Sid>[\dA-F]{8})(?<Id>[\dA-F]{2})(?<Pkt>[\dA-F]{2})(?<Bt>[\dA-F]{2})(?<Adc1>[\dA-F]{4})(?<Adc2>[\dA-F]{4})(?<Mode>[\dA-F]{2})(?<Din>[\dA-F]{2})(?<Dout>[\dA-F]{2})?(?<CHK>[\dA-F]{2})$");
        private static readonly Regex[] regexs = { ADXL345Regex, ButtonRegex };

        public AsciiScanner()
        {
        }

        public TagInfo Scan(string msg)
        {
            TagInfo info = new TagInfo() { Valid = false };
            if (msg.StartsWith(":"))
            {
                foreach (var r in regexs)
                {
                    Match m = r.Match(msg);
                    if (m.Success)
                    {
                        CopyInfo(info, m.Groups, r.GetGroupNames());
                        if (info.Valid &&
                            ((info.Pkt == PacketId.ADXL345) // 加速度センサーが反応した
                            || (info.Pkt == PacketId.BUTTON && ((info.Din ^ info.Mode) & 1) == 0))) // DI1リードスイッチが開いた
                        {
                            info.WolTrigger = true;
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
                        case nameof(TagInfo.Rptr):
                            valid &= uint.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rptr);
                            info.Rptr = rptr;
                            break;
                        case nameof(TagInfo.Lqi):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte lqi);
                            info.Lqi = lqi;
                            break;
                        case nameof(TagInfo.Ct):
                            valid &= ushort.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort ct);
                            info.Ct = ct;
                            break;
                        case nameof(TagInfo.Sid):
                            valid &= uint.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint sid);
                            if (valid)
                            {
                                info.Sid = sid;
                                 info.Serial = (sid & 0x7FFFFFFF);
                            }
                            break;
                        case nameof(TagInfo.Serial):
                            valid &= uint.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint serial);
                            if (valid)
                            {
                                info.Serial = serial;
                                info.Sid = (serial | 0x80000000);
                            }
                            break;
                        case nameof(TagInfo.Id):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte id);
                            info.Id = id;
                            break;
                        case nameof(TagInfo.Pkt):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte pkt);
                            info.Pkt = pkt;
                            break;
                        case nameof(TagInfo.Bt):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte bt);
                            if (valid)
                            {
                                info.Bt = bt;
                                info.Batt = TagInfo.DecodeVolt(bt);
                            }
                            break;
                        case nameof(TagInfo.Adc1):
                            valid &= ushort.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort adc1);
                            info.Adc1 = adc1;
                            break;
                        case nameof(TagInfo.Adc2):
                            valid &= ushort.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort adc2);
                            info.Adc2 = adc2;
                            break;
                        case nameof(TagInfo.Mode):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte mode);
                            info.Mode = mode;
                            break;
                        case nameof(TagInfo.Din):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte din);
                            info.Din = din;
                            break;
                        case nameof(TagInfo.Dout):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte dout);
                            info.Dout = dout;
                            break;
                        case nameof(TagInfo.X):
                            valid &= short.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out short x);
                            info.X = x;
                            break;
                        case nameof(TagInfo.Y):
                            valid &= short.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out short y);
                            info.Y = y;
                            break;
                        case nameof(TagInfo.Z):
                            valid &= short.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out short z);
                            info.Z = z;
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
            info.Valid = valid;
        }
    }
}
