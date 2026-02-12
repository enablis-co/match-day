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
        IMatchWindowService matchWindowService,
        IOfferEvaluationService offerEvaluationService)
    {
        var pub = pubId ?? "PUB-001";
        var context = await matchWindowService.GetMatchWindowContextAsync(time);

        var evaluations = offerEvaluationService.EvaluateAllOffers(context.Timestamp, context.IsActive, context.DemandMultiplier, context.EndTime)
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
                EndsAt = e.Offer.Schedule.EndTime.ToTimeSpan() < TimeOnly.FromDateTime(context.Timestamp).ToTimeSpan()
                    ? (DateTime?)null
                    : context.Timestamp.Date.Add(e.Offer.Schedule.EndTime.ToTimeSpan())
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
            Timestamp = context.Timestamp,
            ActiveOffers = activeOffers,
            SuspendedOffers = suspendedOffers
        });
    }

    private static IResult GetOfferDetails(string offerId, IOfferRepository offerRepository)
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
