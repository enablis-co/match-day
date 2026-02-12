using Staffing.Clients;
using Staffing.Clients.Dtos;
using Staffing.Models;

namespace Staffing.Services;

public class StaffingService : IStaffingService
{
    private const decimal EventsWeight = 0.6m;
    private const decimal StockWeight = 0.3m;
    private const decimal HistoricalWeight = 0.1m;
    private const int MaxAdditionalStaff = 4;

    private static readonly List<HistoryRecord> _history = [];
    private static readonly Lock _historyLock = new();

    private readonly IEventsClient _eventsClient;
    private readonly IStockClient _stockClient;
    private readonly ILogger<StaffingService> _logger;

    public StaffingService(
        IEventsClient eventsClient,
        IStockClient stockClient,
        ILogger<StaffingService> logger)
    {
        _eventsClient = eventsClient;
        _stockClient = stockClient;
        _logger = logger;
    }

    public async Task<StaffingRecommendation> GetRecommendationAsync(string pubId, DateTime? time = null)
    {
        var now = time ?? DateTime.UtcNow;

        // Call Events and Stock services in parallel
        var eventsTask = _eventsClient.GetActiveEventsAsync(time);
        var stockTask = _stockClient.GetStockAlertsAsync(pubId);

        await Task.WhenAll(eventsTask, stockTask);

        var eventsResponse = await eventsTask;
        var stockResponse = await stockTask;

        // Build signals
        var signals = new List<Signal>();
        var eventsAvailable = eventsResponse is not null;
        var stockAvailable = stockResponse is not null && !string.IsNullOrEmpty(stockResponse.PubId);

        // Calculate adjusted weights based on available signals
        var (eventsWeight, stockWeight, historicalWeight) =
            CalculateAdjustedWeights(eventsAvailable, stockAvailable);

        // Demand multiplier signal
        var demandMultiplier = 1.0;
        var matchDescription = string.Empty;
        var matchWindowActive = false;

        if (eventsAvailable)
        {
            var topEvent = eventsResponse!.ActiveEvents.MaxBy(e => e.DemandMultiplier);
            if (topEvent is not null)
            {
                demandMultiplier = topEvent.DemandMultiplier;
                matchDescription = topEvent.Description;
                matchWindowActive = true;
            }

            signals.Add(new Signal
            {
                Source = SignalSource.EVENTS,
                Description = matchWindowActive
                    ? $"Match window active — {matchDescription}"
                    : "No active match window",
                Weight = eventsWeight,
                RawValue = demandMultiplier
            });
        }

        // Stock pressure signal
        var stockPressure = "NONE";
        var stockAlertCount = 0;

        if (stockAvailable)
        {
            stockPressure = stockResponse!.OverallPressure;
            stockAlertCount = stockResponse.AlertCount;

            signals.Add(new Signal
            {
                Source = SignalSource.STOCK,
                Description = stockAlertCount > 0
                    ? $"Stock pressure {stockPressure} — {stockAlertCount} alert(s)"
                    : "No stock alerts",
                Weight = stockWeight,
                RawValue = stockPressure
            });
        }

        // Historical signal (simulated baseline)
        var historicalAverage = CalculateHistoricalAverage(pubId, demandMultiplier);
        signals.Add(new Signal
        {
            Source = SignalSource.HISTORICAL,
            Description = $"Similar events averaged +{historicalAverage:F1} staff",
            Weight = historicalWeight,
            RawValue = historicalAverage
        });

        // Calculate recommendation
        var additionalStaff = CalculateStaffDelta(demandMultiplier, stockPressure);
        var action = additionalStaff > 0
            ? StaffingAction.INCREASE
            : StaffingAction.MAINTAIN;
        var urgency = CalculateUrgency(demandMultiplier, stockPressure);
        var confidence = CalculateConfidence(eventsAvailable, stockAvailable, signals);

        var roles = DetermineRoles(additionalStaff, stockPressure);

        var recommendation = new StaffingRecommendation
        {
            PubId = pubId,
            Timestamp = now,
            Recommendation = new RecommendationDetail
            {
                Action = action,
                AdditionalStaff = additionalStaff,
                Roles = roles,
                Urgency = urgency,
                WindowStart = now,
                WindowEnd = now.AddHours(3)
            },
            Confidence = confidence,
            Signals = signals
        };

        // Store in history
        StoreHistory(pubId, now, matchDescription, additionalStaff);

        return recommendation;
    }

    public async Task<SignalsResponse> GetSignalsAsync(string pubId, DateTime? time = null)
    {
        var now = time ?? DateTime.UtcNow;

        var eventsTask = _eventsClient.GetActiveEventsAsync(time);
        var stockTask = _stockClient.GetStockAlertsAsync(pubId);

        await Task.WhenAll(eventsTask, stockTask);

        var eventsResponse = await eventsTask;
        var stockResponse = await stockTask;

        var demandMultiplier = 1.0;
        var matchDescription = string.Empty;
        var matchWindowActive = false;

        if (eventsResponse is not null)
        {
            var topEvent = eventsResponse.ActiveEvents.MaxBy(e => e.DemandMultiplier);
            if (topEvent is not null)
            {
                demandMultiplier = topEvent.DemandMultiplier;
                matchDescription = topEvent.Description;
                matchWindowActive = true;
            }
        }

        var stockPressure = "NONE";
        var stockAlertCount = 0;

        if (stockResponse is not null && !string.IsNullOrEmpty(stockResponse.PubId))
        {
            stockPressure = stockResponse.OverallPressure;
            stockAlertCount = stockResponse.AlertCount;
        }

        return new SignalsResponse
        {
            PubId = pubId,
            Timestamp = now,
            Signals = new SignalsDetail
            {
                DemandMultiplier = demandMultiplier,
                MatchWindowActive = matchWindowActive,
                MatchDescription = matchDescription,
                StockPressure = stockPressure,
                StockAlerts = stockAlertCount,
                HistoricalAverage = CalculateHistoricalAverage(pubId, demandMultiplier)
            }
        };
    }

    public HistoryResponse GetHistory(string pubId, int days = 7)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");

        lock (_historyLock)
        {
            var records = _history
                .Where(h => h.Date.CompareTo(cutoff) >= 0)
                .OrderByDescending(h => h.Date)
                .ToList();

            return new HistoryResponse
            {
                PubId = pubId,
                Recommendations = records
            };
        }
    }

    private static int CalculateStaffDelta(double demandMultiplier, string stockPressure)
    {
        var staff = 0;

        // Demand-based thresholds
        if (demandMultiplier >= 2.0)
            staff = 2;
        else if (demandMultiplier >= 1.5)
            staff = 1;

        // Stock pressure adds to recommendation
        if (string.Equals(stockPressure, "HIGH", StringComparison.OrdinalIgnoreCase))
            staff += 1;

        // Cap at maximum
        return Math.Min(staff, MaxAdditionalStaff);
    }

    private static Urgency CalculateUrgency(double demandMultiplier, string stockPressure)
    {
        if (demandMultiplier >= 2.0 || string.Equals(stockPressure, "HIGH", StringComparison.OrdinalIgnoreCase))
            return Urgency.HIGH;

        if (demandMultiplier >= 1.5)
            return Urgency.MEDIUM;

        return Urgency.LOW;
    }

    private static (decimal eventsWeight, decimal stockWeight, decimal historicalWeight)
        CalculateAdjustedWeights(bool eventsAvailable, bool stockAvailable)
    {
        // Start with base weights
        var activeWeights = new List<(string name, decimal weight)>();

        if (eventsAvailable)
            activeWeights.Add(("events", EventsWeight));
        if (stockAvailable)
            activeWeights.Add(("stock", StockWeight));

        // Historical is always available
        activeWeights.Add(("historical", HistoricalWeight));

        // Proportionally redistribute so weights sum to 1.0
        var totalActiveWeight = activeWeights.Sum(w => w.weight);

        var eventsW = eventsAvailable ? EventsWeight / totalActiveWeight : 0m;
        var stockW = stockAvailable ? StockWeight / totalActiveWeight : 0m;
        var historicalW = HistoricalWeight / totalActiveWeight;

        return (Math.Round(eventsW, 2), Math.Round(stockW, 2), Math.Round(historicalW, 2));
    }

    private static Confidence CalculateConfidence(bool eventsAvailable, bool stockAvailable, List<Signal> signals)
    {
        // Events is the primary signal (60% weight) — if it's unavailable, confidence is LOW
        if (!eventsAvailable)
            return Confidence.LOW;

        // Stock unavailable but events available → MEDIUM
        if (!stockAvailable)
            return Confidence.MEDIUM;

        // All available and consistent → HIGH
        return Confidence.HIGH;
    }

    private static List<string> DetermineRoles(int additionalStaff, string stockPressure)
    {
        if (additionalStaff == 0)
            return [];

        var roles = new List<string> { "bar" };

        if (additionalStaff >= 2)
            roles.Add("floor");

        if (string.Equals(stockPressure, "HIGH", StringComparison.OrdinalIgnoreCase))
            roles.Add("cellar");

        return roles;
    }

    private static double CalculateHistoricalAverage(string pubId, double demandMultiplier)
    {
        // Simulated historical average based on demand level
        // In production this would come from stored history data
        if (demandMultiplier >= 2.0) return 2.1;
        if (demandMultiplier >= 1.5) return 1.3;
        return 0.2;
    }

    private static void StoreHistory(string pubId, DateTime timestamp, string eventDescription, int recommended)
    {
        lock (_historyLock)
        {
            _history.Add(new HistoryRecord
            {
                Date = timestamp.ToString("yyyy-MM-dd"),
                Event = string.IsNullOrEmpty(eventDescription) ? "No event" : eventDescription,
                Recommended = recommended,
                Outcome = "PENDING",
                Feedback = "Awaiting feedback"
            });

            // Keep only last 100 records to avoid unbounded growth
            if (_history.Count > 100)
                _history.RemoveAt(0);
        }
    }
}
