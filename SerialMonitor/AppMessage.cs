namespace SerialMonitor
{

    public enum AppCommands
    {
        Get,
        Add,
        Remove,
        SetInterval,
        GetInterval,
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
        S_Success,
        S_UnknownCommand,
        S_IncompleteParameters,
        S_NoPhysicalAddress,
        S_EntryNotFound,
        S_InvalidPhysicalFormat
    }

}
