using Pricing.Data;
using Pricing.Models;
using Pricing.Services;

namespace Pricing.Endpoints;

public static class OffersEndpoints
{
    public static void MapOffersEndpoints(this WebApplication app)
    {
        app.MapGet("/offers/active", GetActiveOffers)
            .WithTags("Offers");

        app.MapGet("/offers/{offerId}", GetOfferDetails)
            .WithTags("Offers");
    }

    private static async Task<IResult> GetActiveOffers(
        string? pubId,
        DateTime? time,
        EventsService eventsService,
        OfferEvaluationService offerEvaluationService,
        OfferRepository offerRepository)
    {
        var now = time ?? DateTime.UtcNow;
        var pub = pubId ?? "PUB-001";

        var eventsResponse = await eventsService.GetActiveEventsAsync(time);
        var demandResponse = await eventsService.GetDemandMultiplierAsync(time);

        var matchWindowActive = eventsResponse?.InMatchWindow ?? false;
        var demandMultiplier = demandResponse?.Multiplier ?? 1.0;

        DateTime? matchWindowEnd = null;
        if (eventsResponse?.ActiveEvents?.Any() == true)
        {
            matchWindowEnd = eventsResponse.ActiveEvents
                .Max(e => now.AddMinutes(e.MinutesRemaining + 30));
        }

        var evaluations = offerEvaluationService.EvaluateAllOffers(now, matchWindowActive, demandMultiplier, matchWindowEnd)
            .Where(e => e.Status != OfferStatus.INACTIVE)
            .ToList();

        var activeOffers = evaluations
            .Where(e => e.Status == OfferStatus.ACTIVE)
            .Select(e => new
            {
                e.Offer.OfferId,
                e.Offer.Name,
                e.Offer.Description,
                Status = "ACTIVE",
                EndsAt = e.Offer.Schedule.EndTime.ToTimeSpan() < TimeOnly.FromDateTime(now).ToTimeSpan()
                    ? (DateTime?)null
                    : now.Date.Add(e.Offer.Schedule.EndTime.ToTimeSpan())
            })
            .ToList();

        var suspendedOffers = evaluations
            .Where(e => e.Status is OfferStatus.SUSPENDED or OfferStatus.ENDED_EARLY)
            .Select(e => new
            {
                e.Offer.OfferId,
                e.Offer.Name,
                Reason = e.Reason ?? "Match day rule",
                ResumesAt = e.ResumesAt
            })
            .ToList();

        return Results.Ok(new
        {
            PubId = pub,
            Timestamp = now,
            ActiveOffers = activeOffers,
            SuspendedOffers = suspendedOffers
        });
    }

    private static IResult GetOfferDetails(string offerId, OfferRepository offerRepository)
    {
        var offer = offerRepository.GetById(offerId);
        if (offer is null)
        {
            return Results.NotFound(new { error = $"Offer {offerId} not found" });
        }

        return Results.Ok(new
        {
            offer.OfferId,
            offer.Name,
            offer.Description,
            DiscountType = offer.DiscountType.ToString(),
            offer.DiscountValue,
            offer.ApplicableProducts,
            Schedule = new
            {
                Days = offer.Schedule.Days.Select(d => d.ToString().ToUpper()).ToList(),
                StartTime = offer.Schedule.StartTime.ToString("HH:mm"),
                EndTime = offer.Schedule.EndTime.ToString("HH:mm")
            },
            MatchDayRule = offer.MatchDayRule.ToString()
        });
    }
}
