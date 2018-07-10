using Prism.Mvvm;
using SerialMonitor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace WakeOnDoor.Models
{
    internal class TargetEditorModel: BindableBase
    {
        private const string PKGFAMILY = "TweLiteMonitor-uwp_mtz6gfc7cpfh4";
        private static readonly string[] LANGUAGES = { "en-US", "ja-JP" };
        private const string KEY_LANGUAGE = "language";

        private static TargetEditorModel instance;
        private ApplicationDataContainer settings;

        public ObservableCollection<WOLTarget> WOLTargets { get; }

        private TargetEditorModel()
        {
            WOLTargets = new ObservableCollection<WOLTarget>();
            settings = ApplicationData.Current.LocalSettings;
            if (!settings.Values.ContainsKey(KEY_LANGUAGE))
            {
                settings.Values[KEY_LANGUAGE] = LANGUAGES[0];
            }
        }

        public static TargetEditorModel GetInstance()
        {
            if (instance == null)
            {
                instance = new TargetEditorModel();
            }
            return instance;
        }

        private string language;
        public string Language
        {
            get
            {
                return settings.Values[KEY_LANGUAGE].ToString();
            }
            set
            {
                if (LANGUAGES.Contains(value))
                {
                    settings.Values[KEY_LANGUAGE] = value;
                    SetProperty(ref language, value);
                }
            }
        }

        private string intervalSecStr;
        public string IntervalSecStr
        {
            get
            {
                return intervalSecStr;
            }
            private set
            {
                SetProperty(ref intervalSecStr, value);
            }
        }

        private string statusMessage;
        public string StatusMessage
        {
            get
            {
                return statusMessage;
            }
            private set
            {
                SetProperty(ref statusMessage, value);
            }
        }

        public async Task AddAsync(WOLTarget target)
        {
            using (var conn = await OpenAppServiceAsync())
            {
                if (conn == null) { return; }
                var values = new ValueSet
                {
                    [nameof(Keys.Command)] = nameof(AppCommands.Add),
                    [nameof(Keys.PhysicalAddress)] = target.Physical,
                    [nameof(Keys.Comment)] = target.Comment
                };
                var response = await conn.SendMessageAsync(values);
                if (response.Status == AppServiceResponseStatus.Success)
                {
                    var targetListStr = response.Message[nameof(Keys.TargetList)] as string;
                    RefreshTargetList(targetListStr);
                    var resourceLoader = ResourceLoader.GetForCurrentView();
                    var status = response.Message[nameof(Keys.StatusMessage)] as string;
                    if (!string.IsNullOrEmpty(status)) StatusMessage = resourceLoader.GetString(status);
                }
            }
        }

        public async Task RemoveAsync(WOLTarget target)
        {
            using (var conn = await OpenAppServiceAsync())
            {
                if (conn == null) { return; }
                var values = new ValueSet
                {
                    [nameof(Keys.Command)] = nameof(AppCommands.Remove),
                    [nameof(Keys.PhysicalAddress)] = target.Physical,
                    [nameof(Keys.Comment)] = target.Comment
                };
                var response = await conn.SendMessageAsync(values);
                if (response.Status == AppServiceResponseStatus.Success)
                {
                    var targetListStr = response.Message[nameof(Keys.TargetList)] as string;
                    RefreshTargetList(targetListStr);
                }
            }
        }

        private void RefreshTargetList(string targetJsonStr)
        {
            using (var memStream = new MemoryStream(Encoding.UTF8.GetBytes(targetJsonStr)))
            {
                var serializer = new DataContractJsonSerializer(typeof(HashSet<WOLTarget>));
                var targets = serializer.ReadObject(memStream) as HashSet<WOLTarget>;
                WOLTargets.Clear();
                foreach (var m in targets)
                {
                    WOLTargets.Add(m);
                }
                RaisePropertyChanged(nameof(WOLTargets));
            }
        }

        public async Task GetListAsync()
        {
            using (var conn = await OpenAppServiceAsync())
            {
                if (conn == null) { return; }
                var request = new ValueSet
                {
                    [nameof(Keys.Command)] = nameof(AppCommands.Get),
                };
                var response = await conn.SendMessageAsync(request);
                if (response.Status == AppServiceResponseStatus.Success)
                {
                    var targetListStr = response.Message[nameof(Keys.TargetList)] as string;
                    RefreshTargetList(targetListStr);
                }
            }
        }

        public async Task<int> SetIntervalAsync(int interval)
        {
            using (var conn = await OpenAppServiceAsync())
            {
                if (conn != null)
                {
                    var values = new ValueSet
                    {
                        [nameof(Keys.Command)] = nameof(AppCommands.SetInterval),
                        [nameof(Keys.IntervalSec)] = interval.ToString(),
                    };
                    var response = await conn.SendMessageAsync(values);
                    if (response.Status == AppServiceResponseStatus.Success)
                    {
                        IntervalSecStr = response.Message[nameof(Keys.IntervalSec)] as string;
                    }
                }
            }
            int intervalSec = 0;
            Int32.TryParse(IntervalSecStr, out intervalSec);
            return intervalSec;
        }

        public async Task<int> GetIntervalAsync()
        {
            using (var conn = await OpenAppServiceAsync())
            {
                if (conn != null)
                {
                    var values = new ValueSet
                    {
                        [nameof(Keys.Command)] = nameof(AppCommands.GetInterval),
                    };
                    var response = await conn.SendMessageAsync(values);
                    if (response.Status == AppServiceResponseStatus.Success)
                    {
                        IntervalSecStr = response.Message[nameof(Keys.IntervalSec)] as string;
                    }
                }
            }
            int intervalSec = 0;
            Int32.TryParse(IntervalSecStr, out intervalSec);
            return intervalSec;
        }

        private async Task<AppServiceConnection> OpenAppServiceAsync()
        {
            var conn = new AppServiceConnection
            {
                AppServiceName = "SettingsEditor",
                PackageFamilyName = PKGFAMILY
            };
            var status = await conn.OpenAsync();
            if (status == AppServiceConnectionStatus.Success)
            {
                return conn;
            }
            return null;
        }
    }
}
