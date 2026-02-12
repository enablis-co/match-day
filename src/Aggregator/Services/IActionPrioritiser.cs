using Aggregator.Models;

namespace Aggregator.Services;

public interface IActionPrioritiser
{
    List<PubAction> PrioritiseActions(
        EventSummary? events,
        StockSummary? stock,
        StaffingSummary? staffing,
        PricingSummary? pricing,
        DateTime now);
}
