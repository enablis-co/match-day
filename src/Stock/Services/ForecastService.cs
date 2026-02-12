using Stock.Models;

namespace Stock.Services;

public class ForecastService : IForecastService
{
    private readonly IStockQueryService _stockQueryService;
    private readonly IDemandMultiplierService _demandMultiplierService;
    private readonly IForecastConfidenceStrategy _confidenceStrategy;

    public ForecastService(
        IStockQueryService stockQueryService, 
        IDemandMultiplierService demandMultiplierService,
        IForecastConfidenceStrategy confidenceStrategy)
    {
        _stockQueryService = stockQueryService;
        _demandMultiplierService = demandMultiplierService;
        _confidenceStrategy = confidenceStrategy;
    }

    public async Task<ForecastResult?> GetForecastAsync(string pubId, string productId, int hours = 4)
    {
        var stock = await _stockQueryService.GetProductStockAsync(pubId, productId);
        if (stock?.Product == null) return null;

        // Get demand multiplier from Events Service (default to 1.0 if unavailable)
        var demandResult = await _demandMultiplierService.GetDemandMultiplierAsync();
        
        double adjustedRate = stock.Product.BaseConsumptionRate * demandResult.Multiplier;
        double? hoursRemaining = adjustedRate > 0 ? stock.CurrentLevel / adjustedRate : null;
        
        DateTime? depletionTime = hoursRemaining.HasValue 
            ? DateTime.UtcNow.AddHours(hoursRemaining.Value) 
            : null;
        
        bool willDeplete = hoursRemaining.HasValue && hoursRemaining.Value <= hours;
        
        string confidence = demandResult switch
        {
            { IsDefault: true } => "LOW",
            { Multiplier: >= 2.0 } => "HIGH",
            { Multiplier: >= 1.5 } => "MEDIUM",
            _ => "LOW"
        };

        string recommendation = stock.CurrentLevel switch
        {
            <= 10 => "URGENT: Restock immediately",
            <= 30 when willDeplete => "Restock before match ends",
            _ => "Stock levels adequate"
        };

        return new ForecastResult(
            pubId,
            productId,
            stock.Product.ProductName,
            stock.CurrentLevel,
            stock.Product.BaseConsumptionRate,
            $"{stock.Product.Unit}_per_hour",
            demandResult.Multiplier,
            adjustedRate,
            depletionTime,
            hoursRemaining,
            willDeplete,
            confidence,
            recommendation
        );
    }
}