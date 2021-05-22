namespace WakeOnDoor.ViewModels
{
    public enum PageTokenEnum
    {
        SensorStatus,
        TargetEditor,
        PacketLog,
        Shutdown,
        Restart,
        Settings
    }

    public static class PageTokens
    {
        public const string SensorStatusPage = nameof(PageTokenEnum.SensorStatus);
        public const string TargetEditorPage = nameof(PageTokenEnum.TargetEditor);
        public const string PacketLogPage = nameof(PageTokenEnum.PacketLog);
        public const string ShutdownDialog = nameof(PageTokenEnum.Shutdown);
        public const string RestartDialog = nameof(PageTokenEnum.Restart);
        public const string SettingsPage = nameof(PageTokenEnum.Settings);
    }
}
