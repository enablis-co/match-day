using Pricing.Data;
using Pricing.Models;

namespace Pricing.Services;

public class OfferEvaluationService : IOfferEvaluationService
{
    private readonly IOfferRepository _offerRepository;

    public OfferEvaluationService(IOfferRepository offerRepository)
    {
        _offerRepository = offerRepository;
    }

    public bool IsWithinSchedule(Offer offer, DateTime now)
    {
        var currentDay = now.DayOfWeek;
        var currentTime = TimeOnly.FromDateTime(now);
        return offer.Schedule.Days.Contains(currentDay)
            && currentTime >= offer.Schedule.StartTime
            && currentTime <= offer.Schedule.EndTime;
    }

    public OfferEvaluation EvaluateOffer(Offer offer, DateTime now, bool matchWindowActive, double demandMultiplier, DateTime? matchWindowEnd)
    {
        if (!IsWithinSchedule(offer, now))
        {
            return new OfferEvaluation(offer, OfferStatus.INACTIVE, null, null);
        }

        if (!matchWindowActive)
        {
            return new OfferEvaluation(offer, OfferStatus.ACTIVE, null, null);
        }

        // Match day with demand multiplier > 1.5 — no discounts apply (most restrictive rule)
        if (demandMultiplier > 1.5)
        {
            return offer.MatchDayRule switch
            {
                MatchDayRule.SUSPEND => new OfferEvaluation(offer, OfferStatus.SUSPENDED, "Suspended during match day", matchWindowEnd),
                MatchDayRule.END_EARLY => new OfferEvaluation(offer, OfferStatus.ENDED_EARLY, "Match window started", null),
                MatchDayRule.CONTINUE => new OfferEvaluation(offer, OfferStatus.SUSPENDED, "Demand multiplier too high", matchWindowEnd),
                _ => new OfferEvaluation(offer, OfferStatus.ACTIVE, null, null)
            };
        }

        // Match day with lower demand — apply individual offer rules
        return offer.MatchDayRule switch
        {
            MatchDayRule.SUSPEND => new OfferEvaluation(offer, OfferStatus.SUSPENDED, "Suspended during match day", matchWindowEnd),
            MatchDayRule.END_EARLY => new OfferEvaluation(offer, OfferStatus.ENDED_EARLY, "Match window started", null),
            MatchDayRule.CONTINUE => new OfferEvaluation(offer, OfferStatus.ACTIVE, null, null),
            _ => new OfferEvaluation(offer, OfferStatus.ACTIVE, null, null)
        };
    }

    public List<OfferEvaluation> EvaluateAllOffers(DateTime now, bool matchWindowActive, double demandMultiplier, DateTime? matchWindowEnd)
    {
        return _offerRepository.GetAll()
            .Select(o => EvaluateOffer(o, now, matchWindowActive, demandMultiplier, matchWindowEnd))
            .ToList();
    }
}
