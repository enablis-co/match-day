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
        IMatchWindowService matchWindowService,
        IPricingService pricingService)
    {
        var pub = pubId ?? "PUB-001";
        var context = await matchWindowService.GetMatchWindowContextAsync(time);

        var prices = pricingService.GetCurrentPricing(productId, context.Timestamp, context.IsActive, context.DemandMultiplier, context.EndTime);

        return Results.Ok(new
        {
            PubId = pub,
            Timestamp = context.Timestamp,
            Prices = prices
        });
    }

    private static async Task<IResult> GetMatchDayStatus(
        DateTime? time,
        IMatchWindowService matchWindowService,
        IOfferEvaluationService offerEvaluationService)
    {
        var context = await matchWindowService.GetMatchWindowContextAsync(time);

        var affectedOffers = new List<object>();
        if (context.IsActive)
        {
            var evaluations = offerEvaluationService.EvaluateAllOffers(context.Timestamp, context.IsActive, context.DemandMultiplier, context.EndTime);
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
            Timestamp = context.Timestamp,
            MatchDayActive = context.IsActive,
            AffectedOffers = affectedOffers
        });
    }
}
