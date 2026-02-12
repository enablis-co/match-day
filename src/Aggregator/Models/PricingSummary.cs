namespace Aggregator.Models;

public record PricingSummary(
    int OffersActive,
    int OffersSuspended,
    string? SuspensionReason
);
