﻿using Prism.Commands;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;
using Prism.Windows.Validation;
using SerialMonitor;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Input;
using WakeOnDoor.Models;
using Windows.Storage;

namespace WakeOnDoor.ViewModels
{
    public class TargetEditorPageViewModel : ValidatableBindableBase, INavigationAware
    {
        private const string PKGFAMILY = "TweLiteMonitor-uwp_mtz6gfc7cpfh4";

        private readonly TargetEditorModel EditorModel;
        public ObservableCollection<WOLTarget> WOLTargets { get; }

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
        public ICommand WakeNowCommand { get; }

        public TargetEditorPageViewModel()
        {
            WOLTargets = new ObservableCollection<WOLTarget>();
            EditorModel = TargetEditorModel.GetInstance();

            AddMacCommand = new DelegateCommand(async () => {
                WOLTarget target = new WOLTarget() { Physical = PhysicalToEdit, Comment = CommentToEdit, Address=AddressToEdit, Port=PortToEdit, Delay=DelayToEdit };
                await EditorModel.AddAsync(target);
            });
            RemoveMacCommand = new DelegateCommand(async () => {
                WOLTarget target = new WOLTarget() { Physical = PhysicalToEdit };
                await EditorModel.RemoveAsync(target);
            });
            WakeNowCommand = new DelegateCommand(async () => {
                WOLTarget target = new WOLTarget() { Physical = PhysicalToEdit, Address = AddressToEdit, Port = PortToEdit };
                await EditorModel.WakeNowAsync(target);
            });
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

        private string addressToEdit;
        [RestorableState]
        public string AddressToEdit
        {
            get { return addressToEdit; }
            set { SetProperty(ref addressToEdit, value); }
        }

        private string portToEdit;
        [RestorableState]
        public string PortToEdit
        {
            get { return portToEdit; }
            set { SetProperty(ref portToEdit, value); }
        }

        private string delayToEdit;
        [RestorableState]
        public string DelayToEdit
        {
            get { return delayToEdit; }
            set { SetProperty(ref delayToEdit, value); }
        }

        private string commentToEdit;
        [RestorableState]
        public string CommentToEdit
        {
            get { return commentToEdit; }
            set { SetProperty(ref commentToEdit, value); }
        }

        public WOLTarget SelectedTarget
        {
            set
            {
                if (!string.IsNullOrEmpty(value?.Physical))
                {
                    PhysicalToEdit = value?.Physical;
                    AddressToEdit = value?.Address;
                    PortToEdit = value?.Port;
                    DelayToEdit = value?.Delay;
                    CommentToEdit = value?.Comment;
                }
            }
        }

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            switch (args.PropertyName)
            {
                case nameof(StatusMessage):
                    StatusMessage = EditorModel.StatusMessage;
                    break;
                case nameof(WOLTargets):
                    WOLTargets.Clear();
                    foreach (var m in EditorModel.WOLTargets)
                    {
                        WOLTargets.Add(m);
                    }
                    RaisePropertyChanged(nameof(WOLTargets));
                    break;
                default:
                    break;
                }
        }

        public const string TEMP_PREFIX = "Temp.TargetEditor.";
        public async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (settings.Values.TryGetValue(TEMP_PREFIX + nameof(PhysicalToEdit), out object value))
            {
                PhysicalToEdit = value as string;
            }
            if (settings.Values.TryGetValue(TEMP_PREFIX + nameof(AddressToEdit), out value))
            {
                AddressToEdit = value as string;
            }
            if (settings.Values.TryGetValue(TEMP_PREFIX + nameof(PortToEdit), out value))
            {
                PortToEdit = value as string;
            }
            if (settings.Values.TryGetValue(TEMP_PREFIX + nameof(DelayToEdit), out value))
            {
                DelayToEdit = value as string;
            }
            if (settings.Values.TryGetValue(TEMP_PREFIX + nameof(CommentToEdit), out value))
            {
                CommentToEdit = value as string;
            }
            EditorModel.PropertyChanged += OnModelPropertyChanged;
            await EditorModel.GetListAsync();
        }

        public void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            EditorModel.PropertyChanged -= OnModelPropertyChanged;
        }
    }
}
