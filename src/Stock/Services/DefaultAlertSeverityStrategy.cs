using Stock.Enums;

namespace Stock.Services;

public class DefaultAlertSeverityStrategy : IAlertSeverityStrategy
{
    public AlertSeverity CalculateSeverity(double hoursRemaining)
    {
        return hoursRemaining switch
        {
            < 2 => AlertSeverity.CRITICAL,
            < 4 => AlertSeverity.HIGH,
            < 8 => AlertSeverity.MEDIUM,
            _ => AlertSeverity.LOW
        };
    }

    public string GenerateMessage(string productName, double hoursRemaining, AlertSeverity severity)
    {
        var timeDesc = hoursRemaining < 1 
            ? $"{hoursRemaining * 60:F0} minutes" 
            : $"{hoursRemaining:F1} hours";

        return severity switch
        {
            AlertSeverity.CRITICAL => $"URGENT: {productName} will deplete in {timeDesc}. Restock immediately!",
            AlertSeverity.HIGH => $"WARNING: {productName} will deplete in {timeDesc}. Restock soon.",
            AlertSeverity.MEDIUM => $"NOTICE: {productName} will deplete in {timeDesc}. Plan restock.",
            AlertSeverity.LOW => $"INFO: {productName} running low. Will deplete in {timeDesc}.",
            _ => $"{productName} stock alert"
        };
    }
}
