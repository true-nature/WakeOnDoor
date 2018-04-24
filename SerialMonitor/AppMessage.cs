namespace SerialMonitor
{

    public enum AppCommands
    {
        Get,
        Add,
        Remove,
        Clear,
    }

    public enum Keys
    {
        Command,
        PhysicalAddress,
        Comment,
        TargetList,
        Result,
        StatusMessage,
    }

    public enum CommandStatus
    {
        Success,
        UnknownCommand,
        IncompleteParameters,
        NoPhysicalAddress,
        EntryNotFound,
        InvalidPhysicalFormat
    }

}
