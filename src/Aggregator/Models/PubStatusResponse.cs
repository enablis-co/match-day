using System.Text.Json.Serialization;

namespace Aggregator.Models;

public record PubStatusResponse(
    string PubId,
    string PubName,
    DateTime Timestamp,
    StatusInfo Status,
    EventSummary? Events,
    PricingSummary? Pricing,
    StockSummary? Stock,
    StaffingSummary? Staffing,
    SurgeSummary? Surge,
    SurgeForecast? SurgeForecast,
    List<PubAction> Actions,
    ServiceHealthMap ServiceHealth
);

public record StatusInfo(
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    OverallStatus Overall,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    RiskLevel RiskLevel,
    bool MatchDay
);

public record ServiceHealthMap(
    ServiceHealthEntry Events,
    ServiceHealthEntry Pricing,
    ServiceHealthEntry Stock,
    ServiceHealthEntry Staffing,
    ServiceHealthEntry Surge
);
