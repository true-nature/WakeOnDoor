﻿using Prism.Mvvm;
using SerialMonitor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Collections;

namespace WakeOnDoor.Models
{
    internal class TargetEditorModel: BindableBase
    {
        private const string PKGFAMILY = "TweLiteMonitor-uwp_mtz6gfc7cpfh4";
        public ObservableCollection<WOLTarget> WOLTargets { get; }

        private string statusMessage;

        public TargetEditorModel()
        {
            WOLTargets = new ObservableCollection<WOLTarget>();
        }

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

        public async Task InitializeAsync()
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
