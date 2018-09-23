using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Networking;

namespace SerialMonitor
{
    public sealed class SettingsEditor : IBackgroundTask
    {
        private const int DEFAULT_INTERVAL_SECS = 10;
        private const string DEFAULT_DEST_ADDRESS = "FF02::1";   // Link-Local Scope Multicast Addresses / All Nodes Address
        private const string DEFAULT_PORT = "9";    // Discard protocol
        private BackgroundTaskDeferral taskDeferral;
        private AppServiceConnection connection;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            taskDeferral = taskInstance.GetDeferral();
            taskInstance.Canceled += this.OnCanceled;
            InitSettings();
            var detail = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            connection = detail?.AppServiceConnection;
            if (connection != null)
            {
                connection.RequestReceived += OnRequestReceived;
            }
        }

        internal static IEnumerable<ulong> GetPhysicals()
        {
            var settings = ApplicationData.Current.LocalSettings;
            var targetDic = ReadMacList(settings);
            var physicals = from p in targetDic.Values select ulong.Parse(p.Physical.Replace("-",""));
            return physicals;
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            taskDeferral?.Complete();
            taskDeferral = null;
        }

        private void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var settings = ApplicationData.Current.LocalSettings;
            var targetDic = ReadMacList(settings);

            var message = args.Request.Message;
            var resValues = new ValueSet
            {
                [nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_UnknownCommand),
            };
            if (message.TryGetValue(nameof(Keys.Command), out object command))
            {
                switch (command)
                {
                    case nameof(AppCommands.Add):
                        AddTarget(settings, targetDic, message, resValues);
                        break;
                    case nameof(AppCommands.Remove):
                        RemoveTarget(settings, targetDic, resValues, message);
                        break;
                    case nameof(AppCommands.Wake):
                        WakeTargetAsync(settings, targetDic, message, resValues);
                        break;
                    case nameof(AppCommands.Get):
                        resValues[nameof(Keys.Result)] = true.ToString();
                        resValues[nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_Success);
                        break;
                    case nameof(AppCommands.SetInterval):
                        SetInterval(settings, resValues, message);
                        break;
                    case nameof(AppCommands.GetInterval):
                        GetInterval(settings, resValues, message);
                        break;
                    default:
                        resValues[nameof(Keys.Result)] = false.ToString();
                        break;
                }
            }
            resValues[nameof(Keys.TargetList)] = settings.Values[nameof(Keys.TargetList)];
            var result = args.Request.SendResponseAsync(resValues);
        }

        internal static void InitSettings()
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (!settings.Values.ContainsKey(nameof(Keys.TargetList)))
            {
                SaveMacList(settings, new HashSet<WOLTarget>());
            }
            if (!settings.Values.ContainsKey(nameof(Keys.IntervalSec)))
            {
                settings.Values[nameof(Keys.IntervalSec)] = DEFAULT_INTERVAL_SECS.ToString();
            }
        }

        private static string SaveMacList(ApplicationDataContainer settings, IEnumerable<WOLTarget> targetList)
        {
            using (var stream = new MemoryStream())
            {
                var targetSet = new HashSet<WOLTarget>(targetList);
                var serializer = new DataContractJsonSerializer(typeof(HashSet<WOLTarget>));
                serializer.WriteObject(stream, targetSet);
                var strToSave = Encoding.UTF8.GetString(stream.ToArray());
                settings.Values[nameof(Keys.TargetList)] = strToSave;
                return strToSave;
            }
        }

        private static void AddTarget(ApplicationDataContainer settings, Dictionary<string, WOLTarget> macList, ValueSet message, ValueSet resValues)
        {
            resValues[nameof(Keys.Result)] = false.ToString();
            if (!message.ContainsKey(nameof(Keys.PhysicalAddress)))
            {
                resValues[nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_IncompleteParameters);
                return;
            }
            if (!(message[nameof(Keys.PhysicalAddress)] is string physical))
            {
                resValues[nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_NoPhysicalAddress);
                return;
            }
            if (!NormalizePhysical(ref physical))
            {
                resValues[nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_InvalidPhysicalFormat);
                return;
            }

            if (!message.ContainsKey(nameof(Keys.IpAddress)) || !(message[nameof(Keys.IpAddress)] is string address) || !IsValidAddress(address))
            {
                address = DEFAULT_DEST_ADDRESS;
            }
            if (!message.ContainsKey(nameof(Keys.PortNo)) || !(message[nameof(Keys.PortNo)] is string port) || !IsValidPort(port))
            {
                port = DEFAULT_PORT;
            }

            message.TryGetValue(nameof(Keys.Comment), out object comment);
            var target = new WOLTarget() { Physical = physical as string, Comment = comment as string, Address = address as string, Port = port as string };
            if (string.IsNullOrWhiteSpace(target.Physical))
            {
                resValues[nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_NoPhysicalAddress);
            }
            else
            {
                macList[target.Physical] = target;
                SaveMacList(settings, macList.Values);
                resValues[nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_Success);
                resValues[nameof(Keys.Result)] = true.ToString();
            }
        }

        private static void RemoveTarget(ApplicationDataContainer settings, Dictionary<string, WOLTarget> macList, ValueSet resValues, ValueSet message)
        {
            bool result = false;
            if (!message.ContainsKey(nameof(Keys.PhysicalAddress)))
            {
                resValues[nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_IncompleteParameters);
            }
            else if (!(message[nameof(Keys.PhysicalAddress)] is string physical))
            {
                resValues[nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_NoPhysicalAddress);
            }
            else
            {
                NormalizePhysical(ref physical);

                if (macList.ContainsKey(physical))
                {
                    resValues[nameof(Keys.Result)] = macList.Remove(physical);
                    SaveMacList(settings, macList.Values);
                    resValues[nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_Success);
                    result = true;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(physical))
                    {
                        resValues[nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_NoPhysicalAddress);
                    }
                    else
                    {
                        resValues[nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_EntryNotFound);
                    }
                }
            }
            resValues[nameof(Keys.Result)] = result.ToString();
        }

        private static async void WakeTargetAsync(ApplicationDataContainer settings, Dictionary<string, WOLTarget> macList, ValueSet message, ValueSet resValues)
        {
            if (!message.ContainsKey(nameof(Keys.PhysicalAddress)))
            {
                resValues[nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_IncompleteParameters);
                return;
            }
            if (!(message[nameof(Keys.PhysicalAddress)] is string physical))
            {
                resValues[nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_NoPhysicalAddress);
                return;
            }
            if (!NormalizePhysical(ref physical))
            {
                resValues[nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_InvalidPhysicalFormat);
                return;
            }
            if (!message.ContainsKey(nameof(Keys.IpAddress)) || !(message[nameof(Keys.IpAddress)] is string address))
            {
                address = DEFAULT_DEST_ADDRESS;
            }
            if (!message.ContainsKey(nameof(Keys.PortNo)) || !(message[nameof(Keys.PortNo)] is string port))
            {
                port = DEFAULT_PORT;
            }
            if (!NormalizeDestAddress(address, port))
            {
                resValues[nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_InvalidAddressFormat);
                return;
            }

            if (string.IsNullOrWhiteSpace(physical))
            {
                resValues[nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_NoPhysicalAddress);
            }
            else
            {
                bool result = await WOLHelper.WakeUpAsync(physical.Replace("-", ""), address, port);
                resValues[nameof(Keys.Result)] = result.ToString();
                resValues[nameof(Keys.StatusMessage)] = (result ? nameof(CommandStatus.S_SentWOL) : nameof(CommandStatus.S_IncompleteParameters));
            }
        }

        private static void SetInterval(ApplicationDataContainer settings, ValueSet resValues, ValueSet message)
        {
            bool result = false;
            if (!message.ContainsKey(nameof(Keys.IntervalSec)))
            {
                resValues[nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_IncompleteParameters);
            }
            else if (int.TryParse(message[nameof(Keys.IntervalSec)] as string, out int interval))
            {
                settings.Values[nameof(Keys.IntervalSec)] = interval.ToString();
                resValues[nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_Success);
                result = true;
            }
            else
            {
                resValues[nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_IncompleteParameters);
            }
            resValues[nameof(Keys.IntervalSec)] = settings.Values[nameof(Keys.IntervalSec)];
            resValues[nameof(Keys.Result)] = result.ToString();
        }

        private static void GetInterval(ApplicationDataContainer settings, ValueSet resValues, ValueSet message)
        {
            resValues[nameof(Keys.IntervalSec)] = settings.Values[nameof(Keys.IntervalSec)];
            resValues[nameof(Keys.StatusMessage)] = nameof(CommandStatus.S_Success);
            resValues[nameof(Keys.Result)] = true.ToString();
        }

        /// <summary>
        /// Normalize physical address if matched.
        /// </summary>
        /// <param name="physical"></param>
        /// <returns></returns>
        private static bool NormalizePhysical(ref string physical)
        {
            Regex regex = new Regex(@"^([\dA-F]{2})-?([\dA-F]{2})-?([\dA-F]{2})-?([\dA-F]{2})-?([\dA-F]{2})-?([\dA-F]{2})$");
            physical = physical.Trim().ToUpper().Replace(':', '-');
            var m = regex.Match(physical);
            if (m.Success)
            {
                var builder = new StringBuilder(m.Groups[1].Value);
                for (int i = 2; i < m.Groups.Count; i++)
                {
                    builder.Append('-').Append(m.Groups[i].Value);
                }
                physical = builder.ToString();
            }
            return m.Success;
        }

        private static bool IsValidAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }
            var host = new HostName(address);
            return (host.Type == HostNameType.Ipv4 || host.Type == HostNameType.Ipv6);
        }

        private static bool IsValidPort(string port)
        {
            int portNo = -1;
            var parsed = int.TryParse(port, out portNo);
            parsed &= portNo > 0 && portNo < 65536;
            return (parsed && portNo > 0 && portNo < 65536);
        }

        private static bool NormalizeDestAddress(string address, string port)
        {
            return IsValidAddress(address) && IsValidPort(port);
        }

        internal static Dictionary<string, WOLTarget> ReadMacList(ApplicationDataContainer settings)
        {
            Dictionary<string, WOLTarget> result = new Dictionary<string, WOLTarget>();
            var targetListStr = (settings.Values[nameof(Keys.TargetList)] as string);
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(targetListStr)))
            {
                var serializer = new DataContractJsonSerializer(typeof(HashSet<WOLTarget>));
                var targets = serializer.ReadObject(stream) as HashSet<WOLTarget>;
                foreach (var t in targets)
                {
                    if (!IsValidAddress(t.Address))
                    {
                        t.Address = DEFAULT_DEST_ADDRESS;
                    }
                    if (!IsValidPort(t.Port))
                    {
                        t.Port = DEFAULT_PORT;
                    }
                    result.Add(t.Physical, t);
                }
            }
            return result;
        }
    }
}
