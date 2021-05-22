namespace WakeOnDoor.ViewModels
{
    public class RestartDialogViewModel
    {
        public RestartDialogViewModel()
        {
        }

        public bool IsIoTDeviceFamily => App.IsIoTDeviceFamily;
    }
}
