using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SerialMonitor.Scanner
{
    /// semicolon style
    /// ;23;00000000;141;110;10f1d2f;2760;0008;0000;1025;0586;X;-005;-093;-019;
    /// BUTTON
    /// ;42;00000000;084;37627;10118ca;2930;0964;0571;0001;0001;P;0000;
    internal class SmplTag3Scanner : IMessageScanner
    {
        private static readonly Regex ADXL345Regex = new Regex(@";(?<Ts>\d+);(?<Rptr>[\dA-F]{8});(?<Lqi>\d{3});(?<Ct>\d+);(?<Serial>[\da-f]{7});(?<Batt>\d{4});(?<Mode>[\da-f]+);0000;(?<Adc1>\d+);(?<Adc2>\d+);(?<Flg>X);(?<X>-?\d+);(?<Y>-?\d+);(?<Z>-?\d+);");
        private static readonly Regex ButtonRegex = new Regex(@";(?<Ts>\d+);(?<Rptr>[\dA-F]{8});(?<Lqi>\d{3});(?<Ct>\d+);(?<Serial>[\da-f]{7});(?<Batt>\d+);(?<Adc1>\d+);(?<Adc2>\d+);(?<Mode>\d{4});(?<Din>\d{4});(?<Flg>P);(?<Dout>\d{4});");
        //private static readonly Regex StandardRegex = new Regex(@";(?<Ts>\d+);(?<Rptr>[\dA-F]+);(?<Lqi>\d+);(?<Ct>[\dA-F]+);(?<Serial>[\da-f]{7});(?<Batt>\d+);(?<Lm61>\d+);(?<Cap>\d+);(?<Adc1>\d+);(?<Adc2>\d+);(?<Flg>S);");
        private static readonly Regex[] regexs = { ADXL345Regex, ButtonRegex };

        public SmplTag3Scanner()
        {
        }

        public TagInfo Scan(string msg)
        {
            TagInfo info = new TagInfo() { Valid = false };
            if (msg.StartsWith(";"))
            {
                foreach (var r in regexs)
                {
                    Match m = r.Match(msg);
                    if (m.Success)
                    {
                        CopyInfo(info, m.Groups, r.GetGroupNames());
                        info.Pkt = Flag2Pkt(m.Groups["flg"].Value);
                        if (info.Valid && !(info.Pkt == 0xFE && ((info.Din ^ info.Mode) & 1) == 0))
                        {
                            info.WolTrigger = true;
                            break;
                        }
                    }
                }
            }
            return info;
        }

        private byte Flag2Pkt(string flag)
        {
            byte pkt = 0;
            if (!string.IsNullOrWhiteSpace(flag))
            {
                switch (flag)
                {
                    case "X":
                        pkt = 0x35;
                        break;
                    case "P":
                        pkt = 0xFE;
                        break;
                    //case "S":
                    //    pkt = 0x10;
                    //    break;
                }
            }
            return pkt;
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
                        case nameof(TagInfo.Ts):
                            valid &= uint.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint ts);
                            info.Ts = ts;
                            break;
                        case nameof(TagInfo.Rptr):
                            valid &= uint.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rptr);
                            info.Rptr = rptr;
                            break;
                        case nameof(TagInfo.Lqi):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte lqi);
                            info.Lqi = lqi;
                            break;
                        case nameof(TagInfo.Ct):
                            valid &= ushort.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort ct);
                            info.Ct = ct;
                            break;
                        case nameof(TagInfo.Serial):
                            valid &= uint.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint serial);
                            if (valid) {
                                info.Serial = serial;
                                info.Sid = (serial | 0x80000000);
                            }
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
