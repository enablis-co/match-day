namespace Stock.Enums;

public enum AlertSeverity
{
    CRITICAL,   // < 2 hours
    HIGH,       // < 4 hours
    MEDIUM,     // < 8 hours
    LOW         // < 12 hours
}