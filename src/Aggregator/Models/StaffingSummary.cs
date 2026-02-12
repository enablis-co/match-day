namespace Aggregator.Models;

public record StaffingSummary(
    string Recommendation,
    int AdditionalRequired,
    string Urgency,
    string Confidence
);
