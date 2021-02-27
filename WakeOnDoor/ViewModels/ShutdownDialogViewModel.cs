namespace WakeOnDoor.ViewModels
{
    public class ShutdownDialogViewModel
    {
        public ShutdownDialogViewModel()
        {
        }

        public bool IsIoTDeviceFamily => App.IsIoTDeviceFamily;
    }
}
