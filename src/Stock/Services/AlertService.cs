using Microsoft.EntityFrameworkCore;
using Stock.Data;
using Stock.Enums;
using Stock.Models;

namespace Stock.Services;

public class AlertService : IAlertService
{
    private readonly StockDbContext _context;
    private readonly IDemandMultiplierService _demandMultiplierService;
    private readonly IAlertSeverityStrategy _severityStrategy;
    private readonly IStockCalculationService _calculationService;

    public AlertService(
        StockDbContext context, 
        IDemandMultiplierService demandMultiplierService,
        IAlertSeverityStrategy severityStrategy,
        IStockCalculationService calculationService)
    {
        _context = context;
        _demandMultiplierService = demandMultiplierService;
        _severityStrategy = severityStrategy;
        _calculationService = calculationService;
    }

    public async Task<IEnumerable<StockAlert>> GetStockAlertsAsync(string pubId, double hoursThreshold = 12)
    {
        var stockLevelsTask = _context.StockLevels
            .Include(s => s.Product)
            .Where(s => s.PubId == pubId)
            .ToListAsync();

        var demandResultTask = _demandMultiplierService.GetDemandMultiplierAsync();

        await Task.WhenAll(stockLevelsTask, demandResultTask);

        var stockLevels = await stockLevelsTask;
        var demandResult = await demandResultTask;
        var alerts = new List<StockAlert>();

        foreach (var stock in stockLevels)
        {
            if (stock.Product == null) continue;

            var adjustedRate = stock.Product.BaseConsumptionRate * demandResult.Multiplier;
            var hoursRemaining = adjustedRate > 0 ? stock.CurrentLevel / adjustedRate : (double?)null;

            // Only create alert if within threshold
            if (hoursRemaining.HasValue && hoursRemaining.Value <= hoursThreshold)
            {
                var severity = _severityStrategy.CalculateSeverity(hoursRemaining.Value);
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
                    _severityStrategy.GenerateMessage(stock.Product.ProductName, hoursRemaining.Value, severity)
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

    public async Task<IEnumerable<StockLevel>> GetLowStockAlertsAsync(string pubId, double threshold = 30)
    {
        return await _context.StockLevels
            .Include(s => s.Product)
            .Where(s => s.PubId == pubId && s.CurrentLevel <= threshold)
            .ToListAsync();
    }
}