using System.Diagnostics;
using Aggregator.Clients;
using Aggregator.Models;

namespace Aggregator.Services;

public class AggregatorService : IAggregatorService
{
    private readonly IEventsClient _eventsClient;
    private readonly IPricingClient _pricingClient;
    private readonly IStockClient _stockClient;
    private readonly IStaffingClient _staffingClient;
    private readonly ISurgeClient _surgeClient;
    private readonly IRiskCalculator _riskCalculator;
    private readonly IActionPrioritiser _actionPrioritiser;
    private readonly ILogger<AggregatorService> _logger;

    public AggregatorService(
        IEventsClient eventsClient,
        IPricingClient pricingClient,
        IStockClient stockClient,
        IStaffingClient staffingClient,
        ISurgeClient surgeClient,
        IRiskCalculator riskCalculator,
        IActionPrioritiser actionPrioritiser,
        ILogger<AggregatorService> logger)
    {
        _eventsClient = eventsClient;
        _pricingClient = pricingClient;
        _stockClient = stockClient;
        _staffingClient = staffingClient;
        _surgeClient = surgeClient;
        _riskCalculator = riskCalculator;
        _actionPrioritiser = actionPrioritiser;
        _logger = logger;
    }

    public async Task<PubStatusResponse> GetStatusAsync(string pubId, DateTime? time)
    {
        var now = time ?? DateTime.UtcNow;
        var (events, eventsHealth, pricing, pricingHealth, stock, stockHealth, staffing, staffingHealth, surge, surgeForecast, surgeHealth) =
            await CallAllServicesAsync(pubId, time);

        var matchDay = events?.Active ?? false;
        var riskLevel = _riskCalculator.CalculateRiskLevel(events, stock, staffing);
        var overallStatus = _riskCalculator.CalculateOverallStatus(matchDay, stock, staffing);
        var actions = _actionPrioritiser.PrioritiseActions(events, stock, staffing, pricing, now);

        return new PubStatusResponse(
            PubId: pubId,
            PubName: GetPubName(pubId),
            Timestamp: now,
            Status: new StatusInfo(overallStatus, riskLevel, matchDay),
            Events: events,
            Pricing: pricing,
            Stock: stock,
            Staffing: staffing,
            Surge: surge,
            SurgeForecast: surgeForecast,
            Actions: actions,
            ServiceHealth: new ServiceHealthMap(eventsHealth, pricingHealth, stockHealth, staffingHealth, surgeHealth)
        );
    }

    public async Task<SummaryResponse> GetSummaryAsync(string pubId, DateTime? time)
    {
        var status = await GetStatusAsync(pubId, time);

        var topAction = status.Actions.Count > 0
            ? FormatTopAction(status.Actions[0])
            : null;

        return new SummaryResponse(
            PubId: pubId,
            Timestamp: status.Timestamp,
            MatchDay: status.Status.MatchDay,
            RiskLevel: status.Status.RiskLevel.ToString(),
            ActionCount: status.Actions.Count,
            TopAction: topAction
        );
    }

    public async Task<ActionsResponse> GetActionsAsync(string pubId, DateTime? time)
    {
        var status = await GetStatusAsync(pubId, time);

        return new ActionsResponse(
            PubId: pubId,
            Timestamp: status.Timestamp,
            Actions: status.Actions
        );
    }

    public async Task<HealthResponse> GetHealthAsync()
    {
        var eventsHealth = await CheckServiceHealthAsync("Events", () => _eventsClient.GetActiveEventsAsync(null));
        var pricingHealth = await CheckServiceHealthAsync("Pricing", () => _pricingClient.GetActiveOffersAsync("PUB-001", null));
        var stockHealth = await CheckServiceHealthAsync("Stock", () => _stockClient.GetStockAlertsAsync("PUB-001"));
        var staffingHealth = await CheckServiceHealthAsync("Staffing", () => _staffingClient.GetRecommendationAsync("PUB-001", null));
        var surgeHealth = await CheckServiceHealthAsync("Surge", () => _surgeClient.GetPeakAsync("PUB-001"));

        var allOk = new[] { eventsHealth, pricingHealth, stockHealth, staffingHealth, surgeHealth }
            .All(h => h.Status == ServiceStatus.OK);

        return new HealthResponse(
            Status: allOk ? "HEALTHY" : "DEGRADED",
            Services: new ServiceHealthMap(eventsHealth, pricingHealth, stockHealth, staffingHealth, surgeHealth)
        );
    }

    private async Task<(
        EventSummary? Events, ServiceHealthEntry EventsHealth,
        PricingSummary? Pricing, ServiceHealthEntry PricingHealth,
        StockSummary? Stock, ServiceHealthEntry StockHealth,
        StaffingSummary? Staffing, ServiceHealthEntry StaffingHealth,
        SurgeSummary? Surge, SurgeForecast? SurgeForecast, ServiceHealthEntry SurgeHealth)>
        CallAllServicesAsync(string pubId, DateTime? time)
    {
        var eventsTask = CallWithTimingAsync("Events", () => _eventsClient.GetActiveEventsAsync(time));
        var pricingTask = CallWithTimingAsync("Pricing", () => _pricingClient.GetActiveOffersAsync(pubId, time));
        var stockTask = CallWithTimingAsync("Stock", () => _stockClient.GetStockAlertsAsync(pubId));
        var staffingTask = CallWithTimingAsync("Staffing", () => _staffingClient.GetRecommendationAsync(pubId, time));
        var surgeTask = CallWithTimingAsync("Surge", async () => (await _surgeClient.GetPeakAsync(pubId))!);
        var forecastTask = CallWithTimingAsync("SurgeForecast", async () => (await _surgeClient.GetForecastAsync(pubId, 12))!);

        await Task.WhenAll(eventsTask, pricingTask, stockTask, staffingTask, surgeTask, forecastTask);

        var (events, eventsHealth) = await eventsTask;
        var (pricing, pricingHealth) = await pricingTask;
        var (stock, stockHealth) = await stockTask;
        var (staffing, staffingHealth) = await staffingTask;
        var (surge, surgeHealth) = await surgeTask;
        var (forecast, _) = await forecastTask;

        // Use the worse health of peak + forecast for the single "Surge" health entry
        var combinedSurgeHealth = surgeHealth.Status == ServiceStatus.OK ? surgeHealth : surgeHealth;

        return (events, eventsHealth, pricing, pricingHealth, stock, stockHealth, staffing, staffingHealth, surge, forecast, combinedSurgeHealth);
    }

    private async Task<(T? Result, ServiceHealthEntry Health)> CallWithTimingAsync<T>(
        string serviceName, Func<Task<T>> call) where T : class
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await call();
            sw.Stop();
            _logger.LogInformation("{Service} responded in {Latency}ms", serviceName, sw.ElapsedMilliseconds);
            return (result, new ServiceHealthEntry(ServiceStatus.OK, sw.ElapsedMilliseconds));
        }
        catch (TaskCanceledException)
        {
            sw.Stop();
            _logger.LogWarning("{Service} timed out after {Latency}ms", serviceName, sw.ElapsedMilliseconds);
            return (null, new ServiceHealthEntry(ServiceStatus.DEGRADED, sw.ElapsedMilliseconds));
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "{Service} failed after {Latency}ms", serviceName, sw.ElapsedMilliseconds);
            return (null, new ServiceHealthEntry(ServiceStatus.DEGRADED, sw.ElapsedMilliseconds));
        }
    }

    private async Task<ServiceHealthEntry> CheckServiceHealthAsync<T>(
        string serviceName, Func<Task<T>> call)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await call();
            sw.Stop();
            return new ServiceHealthEntry(ServiceStatus.OK, sw.ElapsedMilliseconds);
        }
        catch
        {
            sw.Stop();
            return new ServiceHealthEntry(ServiceStatus.DEGRADED, sw.ElapsedMilliseconds);
        }
    }

    private static string FormatTopAction(PubAction action)
    {
        var description = action.Action.Replace("_", " ");
        description = char.ToUpper(description[0]) + description[1..].ToLower();

        return action.Deadline.HasValue
            ? $"{description} before {action.Deadline.Value:HH:mm}"
            : description;
    }

    private static string GetPubName(string pubId) => pubId switch
    {
        "PUB-001" => "The Crown & Anchor",
        "PUB-002" => "The Red Lion",
        "PUB-003" => "The White Hart",
        _ => pubId
    };
}
