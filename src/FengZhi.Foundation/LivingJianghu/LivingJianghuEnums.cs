namespace FengZhi.Foundation.LivingJianghu;

public enum WorldEventType
{
    Rumor = 1,
    Code = 2,
    Letter = 3,
    Delegation = 4,
    WorldEvent = 5
}

public enum WorldEventState
{
    Inactive,
    Pending,
    Triggered,
    Delivered,
    Expired
}

public enum DeliveryMethod
{
    Storyteller,
    Traveler,
    Letter,
    CompanionReport,
    Ambient
}

public enum Season
{
    Spring,
    Summer,
    Autumn,
    Winter
}
