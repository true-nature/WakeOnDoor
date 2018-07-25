using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SerialMonitor.Scanner
{
    internal sealed class PacketId
    {
        public const byte STANDARD = 0x10;
        public const byte ADXL345 = 0x35;
        public const byte BUTTON = 0xFE;
    }

    /// <summary>
    /// Scanned tag info
    /// </summary>
    public sealed class TagInfo
    {
        public TagInfo() { Timestamp = DateTimeOffset.Now; }

        /// <summary>
        /// Validity of this TagInfo.
        /// </summary>
        public bool Valid { get; set; }
        /// <summary>
        /// WOL packet should be sent.
        /// </summary>
        public bool WolTrigger { get; set; }

        /// <summary>
        /// timestamp
        /// </summary>
        public uint Ts { get; set; }
        /// <summary>
        /// sid of repeater.
        /// </summary>
        public uint Rptr { get; set; }
        /// <summary>
        /// Link Quality Indicator
        /// </summary>
        public byte Lqi { get; set; }
        /// <summary>
        /// Count
        /// </summary>
        public ushort Ct { get; set; }
        /// <summary>
        /// sid of input endpoint.
        /// </summary>
        public uint Sid { get; set; }
        /// <summary>
        /// serial of input endpoint.
        /// </summary>
        public uint Serial { get; set; }
        /// <summary>
        /// logical id
        /// </summary>
        public byte Id { get; set; }
        /// <summary>
        /// packet type
        /// </summary>
        public byte Pkt { get; set; }
        /// <summary>
        /// encoded battery level
        /// </summary>
        public byte Bt { get; set; }
        /// <summary>
        /// battery level
        /// </summary>
        public ushort Batt { get; set; }
        /// <summary>
        /// ADC1
        /// </summary>
        public ushort Adc1 { get; set; }
        /// <summary>
        /// ADC2
        /// </summary>
        public ushort Adc2 { get; set; }
        /// <summary>
        /// sensor mode
        /// </summary>
        /// <remarks>BUTTON Input(Pkt=0xFE)の場合の動作モード。
        /// 0:DI1（DIO12）の立ち下がりを検出する, 1:DI1（DIO12）の立ち上がりを検出する, 2:DI1（DIO12）で立ち下がり、DI2（DIO13）で立ち上がりを検出する</remarks>
        public byte Mode { get; set; }
        /// <summary>
        /// DI status
        /// </summary>
        public byte Din { get; set; }
        /// <summary>
        /// DO status
        /// </summary>
        public byte Dout { get; set; }
        /// <summary>
        /// Acceleration value of axis X
        /// </summary>
        public short X { get; set; }
        /// <summary>
        /// Acceleration value of axis Y
        /// </summary>
        public short Y { get; set; }
        /// <summary>
        /// Acceleration value of axis Z
        /// </summary>
        public short Z { get; set; }
        public DateTimeOffset Timestamp { get; private set; }

        public static ushort DecodeVolt(byte bt)
        {
            var volt = (ushort)(bt <= 170 ? (1950 + bt * 5) : (2800 + (bt - 170) * 10));
            return volt;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("ts={0}", Ts);
            sb.AppendFormat(",rptr={0:X08}", Rptr);
            sb.AppendFormat(",lqi={0}", Lqi);
            sb.AppendFormat(",ct={0}", Ct);
            sb.AppendFormat(",sid={0:X08}", Sid);
            sb.AppendFormat(",id={0}", Id);
            sb.AppendFormat(",pkt={0:X02}", Pkt);
            sb.AppendFormat(",bt={0}", Bt);
            sb.AppendFormat(",batt={0}", Batt);
            sb.AppendFormat(",adc1={0}", Adc1);
            sb.AppendFormat(",adc2={0}", Adc2);
            sb.AppendFormat(",mode={0}", Mode);
            sb.AppendFormat(",din={0}", Din);
            sb.AppendFormat(",dout={0}", Dout);
            sb.AppendFormat(",x={0}", X);
            sb.AppendFormat(",y={0}", Y);
            sb.AppendFormat(",z={0}", Z);
            return sb.ToString();
        }

        private static readonly Regex TagInfoRegex = new Regex(@"ts=(?<Ts>\d+),rptr=(?<Rptr>[\dA-F]{8}),lqi=(?<Lqi>\d+),ct=(?<Ct>\d+),sid=(?<Sid>[\dA-F]{8}),id=(?<Id>\d+),pkt=(?<Pkt>[\dA-F]{2}),bt=(?<Bt>\d+),batt=(?<Batt>\d+),adc1=(?<Adc1>\d+),adc2=(?<Adc2>\d+),mode=(?<Mode>\d+),din=(?<Din>\d+),dout=(?<Dout>\d+),x=(?<X>-?\d+),y=(?<Y>-?\d+),z=(?<Z>-?\d+)$");
        
        public static TagInfo FromString(string msg)
        {
            TagInfo info = new TagInfo();
            Match m = TagInfoRegex.Match(msg);
            if (m.Success)
            {
                CopyInfo(info, m.Groups, TagInfoRegex.GetGroupNames());
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
                        case nameof(TagInfo.Sid):
                            valid &= uint.TryParse(groups[name].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint sid);
                            if (valid)
                            {
                                info.Sid = sid;
                                info.Serial = (sid & 0x7FFFFFFF);
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
