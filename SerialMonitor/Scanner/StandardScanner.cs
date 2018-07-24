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
        private static readonly Regex ADXL345Regex = new Regex(@"::rc=(?<Relay>[\dA-F]{8}):lq=(?<Lqi>\d+):ct=(?<Ct>[\dA-F]{4}):ed=(?<Sid>[\dA-F]{8}):id=(?<Id>[\dA-F]+):ba=(?<Batt>\d+):a1=(?<Adc1>\d+):a2=(?<Adc2>\d+):x=(?<X>-?\d+):y=(?<Y>-?\d+):z=(?<Z>-?\d+)$");
        //private static readonly Regex ButtonRegex = new Regex(@"::rc=(?<Relay>[\dA-F]{8}):lq=(?<Lqi>\d+):ct=(?<Ct>[\dA-F]{4}):ed=(?<Sid>[\dA-F]{8}):id=(?<Id>[\dA-F]+):ba=(?<Batt>\d+):bt=(?<Dout>\d+)$");
        private static readonly Regex[] regexs = { ADXL345Regex };

        public StandardScanner()
        {
        }

        public TagInfo Scan(string msg)
        {
            TagInfo info = new TagInfo() { Valid = false };
            if (msg.StartsWith("::"))
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
                            if (valid) {
                                info.Sid = sid;
                                info.Serial = (sid & 0x7FFFFFFF);
                            }
                            break;
                        case nameof(TagInfo.Id):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte id);
                            info.Id = id;
                            break;
                        case nameof(TagInfo.Batt):
                            valid &= ushort.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort batt);
                            info.Batt = batt;
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
                            info.Pkt = PacketId.BUTTON;
                            break;
                        case nameof(TagInfo.X):
                            valid &= short.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out short x);
                            info.Pkt = PacketId.ADXL345;
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
