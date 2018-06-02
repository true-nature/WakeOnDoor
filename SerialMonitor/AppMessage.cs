namespace SerialMonitor
{

    public enum AppCommands
    {
        Get,
        Add,
        Remove,
        SetInterval,
    }

    public enum Keys
    {
        Command,
        PhysicalAddress,
        Comment,
        TargetList,
        IntervalSec,
        LastWolTime,
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
