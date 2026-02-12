namespace Pricing.Models;

public record OfferEvaluation(Offer Offer, OfferStatus Status, string? Reason, DateTime? ResumesAt);
