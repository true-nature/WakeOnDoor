using Prism.Commands;
using Prism.Windows.AppModel;
using Prism.Windows.Validation;
using SerialMonitor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Collections;
using Windows.System.Profile;

namespace WakeOnDoor.ViewModels
{
    public class TargetEditorPageViewModel : ValidatableBindableBase
    {
        private const string PKGFAMILY = "TweLiteMonitor-uwp_mtz6gfc7cpfh4";

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

        public ICommand AddMacCommand { get; }
        public ICommand RemoveMacCommand { get; }

        public TargetEditorPageViewModel()
        {
            WOLTargets = new ObservableCollection<WOLTarget>();

            this.AddMacCommand = new DelegateCommand(async () => {
                using (var conn = await OpenAppServiceAsync())
                {
                    if (conn == null) { return; }
                    var values = new ValueSet
                    {
                        [nameof(Keys.Command)] = nameof(AppCommands.Add),
                        [nameof(Keys.PhysicalAddress)] = PhysicalToEdit,
                        [nameof(Keys.Comment)] = CommentToEdit
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
            });
            RemoveMacCommand = new DelegateCommand(async () => {
                using (var conn = await OpenAppServiceAsync())
                {
                    if (conn == null) { return; }
                    var values = new ValueSet
                    {
                        [nameof(Keys.Command)] = nameof(AppCommands.Remove),
                        [nameof(Keys.PhysicalAddress)] = PhysicalToEdit,
                        [nameof(Keys.Comment)] = CommentToEdit
                    };
                    var response = await conn.SendMessageAsync(values);
                    if (response.Status == AppServiceResponseStatus.Success)
                    {
                        var targetListStr = response.Message[nameof(Keys.TargetList)] as string;
                        RefreshTargetList(targetListStr);
                    }
                }
            });

            #pragma warning disable CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
            InitTargetListAsync();
#pragma warning restore CS4014 // この呼び出しを待たないため、現在のメソッドの実行は、呼び出しが完了する前に続行します
        }
        private string physicalToEdit;
        [RestorableState]
        [Required(ErrorMessage = "Please input a Physical Address")]
        [RegularExpression(pattern: @"^[\dA-Fa-f]{2}[\-:]?[\dA-Fa-f]{2}[\-:]?[\dA-Fa-f]{2}[\-:]?[\dA-Fa-f]{2}[\-:]?[\dA-Fa-f]{2}[\-:]?[\dA-Fa-f]{2}$", ErrorMessage = "Illegal format")]
        public string PhysicalToEdit
        {
            get { return physicalToEdit; }
            set { SetProperty(ref physicalToEdit, value); }
        }
        private string commentToEdit;
        [RestorableState]
        public string CommentToEdit
        {
            get { return commentToEdit; }
            set { SetProperty(ref commentToEdit, value); }
        }

        public ObservableCollection<WOLTarget> WOLTargets { get; }

        public WOLTarget SelectedTarget
        {
            set
            {
                if (!string.IsNullOrEmpty(value?.Physical))
                {
                    PhysicalToEdit = value?.Physical;
                    CommentToEdit = value?.Comment;
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

        private async Task InitTargetListAsync()
        {
            using (var conn = await OpenAppServiceAsync())
            {
                if (conn == null) { return; }
                ValueSet request = new ValueSet();
                var response = await conn.SendMessageAsync(request);
                if (response.Status == AppServiceResponseStatus.Success)
                {
                    var targetListStr = response.Message[nameof(Keys.TargetList)] as string;
                    RefreshTargetList(targetListStr);
                }
            }
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
