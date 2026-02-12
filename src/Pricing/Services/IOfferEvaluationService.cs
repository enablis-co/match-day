using Pricing.Models;

namespace Pricing.Services;

public interface IOfferEvaluationService
{
    bool IsWithinSchedule(Offer offer, DateTime now);
    OfferEvaluation EvaluateOffer(Offer offer, DateTime now, bool matchWindowActive, double demandMultiplier, DateTime? matchWindowEnd);
    List<OfferEvaluation> EvaluateAllOffers(DateTime now, bool matchWindowActive, double demandMultiplier, DateTime? matchWindowEnd);
}
