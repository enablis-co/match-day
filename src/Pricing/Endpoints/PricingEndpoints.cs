using Pricing.Models;
using Pricing.Services;

namespace Pricing.Endpoints;

public static class PricingEndpoints
{
    public static void MapPricingEndpoints(this WebApplication app)
    {
        app.MapGet("/pricing/current", GetCurrentPricing)
            .WithTags("Pricing");

        app.MapGet("/pricing/match-day-status", GetMatchDayStatus)
            .WithTags("Pricing");
    }

    private static async Task<IResult> GetCurrentPricing(
        string? pubId,
        string? productId,
        DateTime? time,
        EventsService eventsService,
        PricingService pricingService)
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

        var prices = pricingService.GetCurrentPricing(productId, now, matchWindowActive, demandMultiplier, matchWindowEnd);

        return Results.Ok(new
        {
            PubId = pub,
            Timestamp = now,
            Prices = prices
        });
    }

    private static async Task<IResult> GetMatchDayStatus(
        DateTime? time,
        EventsService eventsService,
        OfferEvaluationService offerEvaluationService)
    {
        var now = time ?? DateTime.UtcNow;

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

        var affectedOffers = new List<object>();
        if (matchWindowActive)
        {
            var evaluations = offerEvaluationService.EvaluateAllOffers(now, matchWindowActive, demandMultiplier, matchWindowEnd);
            foreach (var eval in evaluations)
            {
                if (eval.Status is OfferStatus.SUSPENDED or OfferStatus.ENDED_EARLY)
                {
                    affectedOffers.Add(new
                    {
                        eval.Offer.OfferId,
                        eval.Offer.Name,
                        Action = eval.Status.ToString(),
                        Reason = eval.Reason ?? "Match day rule"
                    });
                }
            }
        }

        return Results.Ok(new
        {
            Timestamp = now,
            MatchDayActive = matchWindowActive,
            AffectedOffers = affectedOffers
        });
    }
}
