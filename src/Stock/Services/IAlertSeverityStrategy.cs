using Stock.Enums;

namespace Stock.Services;

public interface IAlertSeverityStrategy
{
    AlertSeverity CalculateSeverity(double hoursRemaining);
    string GenerateMessage(string productName, double hoursRemaining, AlertSeverity severity);
}
