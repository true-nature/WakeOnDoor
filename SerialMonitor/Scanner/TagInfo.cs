using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SerialMonitor.Scanner
{
    /// <summary>
    /// Scanned tag info
    /// </summary>
    internal class TagInfo
    {
        /// <summary>
        /// Validity of this TagInfo.
        /// </summary>
        public bool valid;
        /// <summary>
        /// WOL packet should be sent.
        /// </summary>
        public bool wolTrigger;

        /// <summary>
        /// timestamp
        /// </summary>
        public uint ts;
        /// <summary>
        /// sid of repeater.
        /// </summary>
        public uint rptr;
        /// <summary>
        /// Link Quality Indicator
        /// </summary>
        public byte lqi;
        /// <summary>
        /// Count
        /// </summary>
        public ushort ct;
        /// <summary>
        /// sid of input endpoint.
        /// </summary>
        public uint sid;
        /// <summary>
        /// serial of input endpoint.
        /// </summary>
        public uint serial;
        /// <summary>
        /// logical id
        /// </summary>
        public byte id;
        /// <summary>
        /// packet type
        /// </summary>
        public byte pkt;
        /// <summary>
        /// encoded battery level
        /// </summary>
        public byte bt;
        /// <summary>
        /// battery level
        /// </summary>
        public ushort batt;
        /// <summary>
        /// ADC1
        /// </summary>
        public ushort adc1;
        /// <summary>
        /// ADC2
        /// </summary>
        public ushort adc2;
        /// <summary>
        /// sensor mode
        /// </summary>
        public byte mode;
        /// <summary>
        /// DI status
        /// </summary>
        public byte din;
        /// <summary>
        /// DO status
        /// </summary>
        public byte dout;
        /// <summary>
        /// Acceleration value of axis X
        /// </summary>
        public short x;
        /// <summary>
        /// Acceleration value of axis Y
        /// </summary>
        public short y;
        /// <summary>
        /// Acceleration value of axis Z
        /// </summary>
        public short z;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("ts={0}", ts);
            sb.AppendFormat(",rptr={0:X08}", rptr);
            sb.AppendFormat(",lqi={0}", lqi);
            sb.AppendFormat(",ct={0}", ct);
            sb.AppendFormat(",sid={0:X08}", sid);
            sb.AppendFormat(",id={0}", id);
            sb.AppendFormat(",pkt={0}", pkt);
            sb.AppendFormat(",bt={0}", bt);
            sb.AppendFormat(",batt={0}", batt);
            sb.AppendFormat(",adc1={0}", adc1);
            sb.AppendFormat(",adc2={0}", adc2);
            sb.AppendFormat(",mode={0}", mode);
            sb.AppendFormat(",din={0}", din);
            sb.AppendFormat(",dout={0}", dout);
            sb.AppendFormat(",x={0}", x);
            sb.AppendFormat(",y={0}", y);
            sb.AppendFormat(",z={0}", z);
            return sb.ToString();
        }

        public static ushort DecodeVolt(byte bt)
        {
            var volt = (ushort)(bt <= 170 ? (1950 + bt * 5) : (2800 + (bt - 170) * 10));
            return volt;
        }
    }
}
