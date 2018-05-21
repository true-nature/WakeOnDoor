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
        private static readonly Regex ADXL345Regex = new Regex(@";(?<ts>\d+);(?<rptr>[\dA-F]{8});(?<lqi>\d{3});(?<ct>\d+);(?<serial>[\da-f]{7});(?<batt>\d{4});(?<mode>[\da-f]+);0000;(?<adc1>\d+);(?<adc2>\d+);(?<flg>X);(?<x>-?\d+);(?<y>-?\d+);(?<z>-?\d+);");
        private static readonly Regex ButtonRegex = new Regex(@";(?<ts>\d+);(?<rptr>[\dA-F]{8});(?<lqi>\d{3});(?<ct>\d+);(?<serial>[\da-f]{7});(?<batt>\d+);(?<adc1>\d+);(?<adc2>\d+);(?<mode>\d{4});(?<din>\d{4});(?<flg>P);(?<dout>\d{4});");
        //private static readonly Regex StandardRegex = new Regex(@";(?<ts>\d+);(?<rptr>[\dA-F]+);(?<lqi>\d+);(?<ct>[\dA-F]+);(?<serial>[\da-f]{7});(?<batt>\d+);(?<lm61>\d+);(?<cap>\d+);(?<adc1>\d+);(?<adc2>\d+);(?<flg>S);");
        private static readonly Regex[] regexs = { ADXL345Regex, ButtonRegex };

        public SmplTag3Scanner()
        {
        }

        public TagInfo Scan(string msg)
        {
            TagInfo info = new TagInfo() { valid = false };
            if (msg.StartsWith(";"))
            {
                foreach (var r in regexs)
                {
                    Match m = r.Match(msg);
                    if (m.Success)
                    {
                        CopyInfo(info, m.Groups, r.GetGroupNames());
                        info.pkt = Flag2Pkt(m.Groups["flg"].Value);
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
                        case nameof(TagInfo.ts):
                            valid &= uint.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out info.ts);
                            break;
                        case nameof(TagInfo.rptr):
                            valid &= uint.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out info.rptr);
                            break;
                        case nameof(TagInfo.lqi):
                            valid &= byte.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out info.lqi);
                            break;
                        case nameof(TagInfo.ct):
                            valid &= ushort.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out info.ct);
                            break;
                        case nameof(TagInfo.serial):
                            valid &= uint.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out info.serial);
                            if (valid) { info.sid = (info.serial | 0x80000000); }
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
                            break;
                        case nameof(TagInfo.x):
                            valid &= short.TryParse(groups[name].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out info.x);
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
