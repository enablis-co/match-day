using Pricing.Models;

namespace Pricing.Services;

public class TracedOfferEvaluationService : IOfferEvaluationService
{
    private readonly IOfferEvaluationService _innerService;
    private readonly ILogger<TracedOfferEvaluationService> _logger;

    public TracedOfferEvaluationService(
        IOfferEvaluationService innerService,
        ILogger<TracedOfferEvaluationService> logger)
    {
        _innerService = innerService;
        _logger = logger;
    }

    public bool IsWithinSchedule(Offer offer, DateTime now)
    {
        _logger.LogDebug(
            "Checking schedule for offer {OfferId}: {OfferName}",
            offer.OfferId,
            offer.Name);

        var result = _innerService.IsWithinSchedule(offer, now);

        _logger.LogDebug(
            "Offer {OfferId} within schedule: {Result}",
            offer.OfferId,
            result);

        return result;
    }

    public OfferEvaluation EvaluateOffer(Offer offer, DateTime now, bool matchWindowActive, double demandMultiplier, DateTime? matchWindowEnd)
    {
        _logger.LogDebug(
            "Evaluating offer {OfferId}: {OfferName} - MatchWindow: {MatchWindowActive}, DemandMultiplier: {DemandMultiplier}",
            offer.OfferId,
            offer.Name,
            matchWindowActive,
            demandMultiplier);

        var result = _innerService.EvaluateOffer(offer, now, matchWindowActive, demandMultiplier, matchWindowEnd);

        _logger.LogDebug(
            "Offer {OfferId} evaluation result: {Status}",
            offer.OfferId,
            result.Status);

        return result;
    }

    public List<OfferEvaluation> EvaluateAllOffers(DateTime now, bool matchWindowActive, double demandMultiplier, DateTime? matchWindowEnd)
    {
        _logger.LogDebug(
            "Evaluating all offers - MatchWindow: {MatchWindowActive}, DemandMultiplier: {DemandMultiplier}",
            matchWindowActive,
            demandMultiplier);

        var result = _innerService.EvaluateAllOffers(now, matchWindowActive, demandMultiplier, matchWindowEnd);

        _logger.LogDebug(
            "Evaluated {OfferCount} offers",
            result.Count);

        return result;
    }
}
