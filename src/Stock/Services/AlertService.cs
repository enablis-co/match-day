using Microsoft.EntityFrameworkCore;
using Stock.Data;
using Stock.Enums;
using Stock.Models;

namespace Stock.Services;

public class AlertService : IAlertService
{
    private readonly StockDbContext _context;
    private readonly IDemandMultiplierService _demandMultiplierService;

    public AlertService(StockDbContext context, IDemandMultiplierService demandMultiplierService)
    {
        _context = context;
        _demandMultiplierService = demandMultiplierService;
    }

    public async Task<IEnumerable<StockAlert>> GetStockAlertsAsync(string pubId, double hoursThreshold = 12)
    {
        var stockLevels = await _context.StockLevels
            .Include(s => s.Product)
            .Where(s => s.PubId == pubId)
            .ToListAsync();

        var demandResult = await _demandMultiplierService.GetDemandMultiplierAsync();
        var alerts = new List<StockAlert>();

        foreach (var stock in stockLevels)
        {
            if (stock.Product == null) continue;

            var adjustedRate = stock.Product.BaseConsumptionRate * demandResult.Multiplier;
            var hoursRemaining = adjustedRate > 0 ? stock.CurrentLevel / adjustedRate : (double?)null;

            // Only create alert if within threshold
            if (hoursRemaining.HasValue && hoursRemaining.Value <= hoursThreshold)
            {
                var severity = CalculateSeverity(hoursRemaining.Value);
                var depletionTime = DateTime.UtcNow.AddHours(hoursRemaining.Value);

                alerts.Add(new StockAlert(
                    pubId,
                    stock.ProductId,
                    stock.Product.ProductName,
                    stock.Product.Category,
                    stock.CurrentLevel,
                    stock.Product.Unit,
                    stock.Product.BaseConsumptionRate,
                    demandResult.Multiplier,
                    adjustedRate,
                    hoursRemaining,
                    depletionTime,
                    severity,
                    GenerateAlertMessage(stock.Product.ProductName, hoursRemaining.Value, severity)
                ));
            }
        }

        return alerts.OrderBy(a => a.HoursRemaining);
    }

    public async Task<IEnumerable<StockAlert>> GetAlertsBySeverityAsync(string pubId, AlertSeverity severity)
    {
        var alerts = await GetStockAlertsAsync(pubId);
        return alerts.Where(a => a.Severity == severity);
    }

    public async Task<IEnumerable<StockAlert>> GetCriticalAlertsAsync(string pubId)
    {
        return await GetAlertsBySeverityAsync(pubId, AlertSeverity.CRITICAL);
    }

    private static AlertSeverity CalculateSeverity(double hoursRemaining)
    {
        return hoursRemaining switch
        {
            < 2 => AlertSeverity.CRITICAL,
            < 4 => AlertSeverity.HIGH,
            < 8 => AlertSeverity.MEDIUM,
            _ => AlertSeverity.LOW
        };
    }

    private static string GenerateAlertMessage(string productName, double hoursRemaining, AlertSeverity severity)
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