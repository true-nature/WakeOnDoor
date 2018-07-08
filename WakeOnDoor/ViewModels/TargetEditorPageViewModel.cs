using Prism.Commands;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;
using Prism.Windows.Validation;
using SerialMonitor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WakeOnDoor.Models;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace WakeOnDoor.ViewModels
{
    public class TargetEditorPageViewModel : ValidatableBindableBase, INavigationAware
    {
        private const string PKGFAMILY = "TweLiteMonitor-uwp_mtz6gfc7cpfh4";

        private TargetEditorModel EditorModel;
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

        public TargetEditorPageViewModel()
        {
            WOLTargets = new ObservableCollection<WOLTarget>();
            EditorModel = new TargetEditorModel();

            AddMacCommand = new DelegateCommand(async () => {
                WOLTarget target = new WOLTarget() { Physical = PhysicalToEdit, Comment = CommentToEdit };
                await EditorModel.AddAsync(target);
            });
            RemoveMacCommand = new DelegateCommand(async () => {
                WOLTarget target = new WOLTarget() { Physical = PhysicalToEdit, Comment = CommentToEdit };
                await EditorModel.RemoveAsync(target);
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
