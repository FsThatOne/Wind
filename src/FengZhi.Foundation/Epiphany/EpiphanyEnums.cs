namespace FengZhi.Foundation.Epiphany;

public enum EpiphanyEventState
{
    Locked,
    Available,
    Triggered,
    Choosing,
    Focusing,
    Completed,
    Skipped,
    Failed,
    Exhausted
}

public enum EpiphanyType
{
    Combat,
    Narrative,
    Meditation
}

public enum EpiphanyChoice
{
    Focus,
    Skip
}
