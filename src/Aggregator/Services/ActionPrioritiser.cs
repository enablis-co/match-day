using Aggregator.Models;

namespace Aggregator.Services;

public class ActionPrioritiser : IActionPrioritiser
{
    public List<PubAction> PrioritiseActions(
        EventSummary? events,
        StockSummary? stock,
        StaffingSummary? staffing,
        PricingSummary? pricing,
        DateTime now)
    {
        var actions = new List<(int SortOrder, PubAction Action)>();

        // Priority 1 & 3: Stock depletion actions
        if (stock is { AlertCount: > 0, CriticalItems.Count: > 0 })
        {
            DateTime? deadline = null;
            if (stock.EstimatedShortfall != null && TimeOnly.TryParse(stock.EstimatedShortfall, out var shortfallTime))
            {
                deadline = now.Date.Add(shortfallTime.ToTimeSpan());
            }

            var hoursUntilDeadline = deadline.HasValue
                ? (deadline.Value - now).TotalHours
                : double.MaxValue;

            foreach (var item in stock.CriticalItems)
            {
                var itemAction = $"RESTOCK_{item.ToUpperInvariant().Replace(" ", "_")}";
                var reason = deadline.HasValue
                    ? $"Will deplete before {deadline.Value:HH:mm}"
                    : $"{item} stock critically low";

                // Sort order: < 1 hour = priority 1, < 2 hours = priority 3
                var sortOrder = hoursUntilDeadline < 1 ? 1 : 3;

                actions.Add((sortOrder, new PubAction(
                    Priority: 0, // Will be assigned after sorting
                    Action: itemAction,
                    Reason: reason,
                    Deadline: deadline,
                    Source: ActionSource.STOCK
                )));
            }
        }

        // Priority 2: Staffing increase with urgency HIGH
        if (staffing != null &&
            string.Equals(staffing.Recommendation, "INCREASE", StringComparison.OrdinalIgnoreCase))
        {
            var isHighUrgency = string.Equals(staffing.Urgency, "HIGH", StringComparison.OrdinalIgnoreCase);
            var sortOrder = isHighUrgency ? 2 : 4;

            actions.Add((sortOrder, new PubAction(
                Priority: 0,
                Action: "CALL_EXTRA_STAFF",
                Reason: $"High demand expected — {staffing.AdditionalRequired} additional staff needed",
                Deadline: null,
                Source: ActionSource.STAFFING
            )));
        }

        // Priority 5: Pricing/offer notifications
        if (pricing is { OffersSuspended: > 0 })
        {
            actions.Add((5, new PubAction(
                Priority: 0,
                Action: "REVIEW_SUSPENDED_OFFERS",
                Reason: pricing.SuspensionReason ?? "Offers suspended",
                Deadline: null,
                Source: ActionSource.PRICING
            )));
        }

        // Sort and assign sequential priorities
        var sorted = actions.OrderBy(a => a.SortOrder).ToList();
        return sorted.Select((a, index) => a.Action with { Priority = index + 1 }).ToList();
    }
}
