namespace Staffing.Models;

public enum StaffingAction
{
    INCREASE,
    DECREASE,
    MAINTAIN
}

public enum Urgency
{
    LOW,
    MEDIUM,
    HIGH
}

public enum Confidence
{
    LOW,
    MEDIUM,
    HIGH
}

public enum SignalSource
{
    EVENTS,
    STOCK,
    HISTORICAL,
    WEATHER
}
