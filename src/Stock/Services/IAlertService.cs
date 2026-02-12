using Stock.Enums;
using Stock.Models;

namespace Stock.Services;

public interface IAlertService
{
    Task<IEnumerable<StockAlert>> GetStockAlertsAsync(string pubId, double hoursThreshold = 12);
    Task<IEnumerable<StockAlert>> GetAlertsBySeverityAsync(string pubId, AlertSeverity severity);
    Task<IEnumerable<StockAlert>> GetCriticalAlertsAsync(string pubId);
}