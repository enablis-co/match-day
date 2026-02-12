using Stock.Models;

namespace Stock.Services;

public class ForecastService : IForecastService
{
    private readonly IStockQueryService _stockQueryService;
    private readonly IDemandMultiplierService _demandMultiplierService;
    private readonly IForecastConfidenceStrategy _confidenceStrategy;
    private readonly IStockCalculationService _calculationService;

    public ForecastService(
        IStockQueryService stockQueryService, 
        IDemandMultiplierService demandMultiplierService,
        IForecastConfidenceStrategy confidenceStrategy,
        IStockCalculationService calculationService)
    {
        _stockQueryService = stockQueryService;
        _demandMultiplierService = demandMultiplierService;
        _confidenceStrategy = confidenceStrategy;
        _calculationService = calculationService;
    }

    public async Task<ForecastResult?> GetForecastAsync(string pubId, string productId, int hours = 4)
    {
        var stockTask = _stockQueryService.GetProductStockAsync(pubId, productId);
        var demandResultTask = _demandMultiplierService.GetDemandMultiplierAsync();

        var stock = await stockTask;
        if (stock?.Product == null) return null;

        var demandResult = await demandResultTask;
        
        double adjustedRate = _calculationService.CalculateAdjustedRate(stock.Product.BaseConsumptionRate, demandResult.Multiplier);
        double? hoursRemaining = _calculationService.CalculateHoursRemaining(stock.CurrentLevel, adjustedRate);
        DateTime? depletionTime = _calculationService.CalculateDepletionTime(hoursRemaining);
        bool willDeplete = _calculationService.WillDeplete(hoursRemaining, hours);
        
        string confidence = _confidenceStrategy.CalculateConfidence(demandResult, stock.CurrentLevel, willDeplete);
        string recommendation = _confidenceStrategy.GenerateRecommendation(stock.CurrentLevel, willDeplete);

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