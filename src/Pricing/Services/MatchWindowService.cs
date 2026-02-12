using Pricing.Models.Dtos;

namespace Pricing.Services;

public interface IMatchWindowService
{
    Task<MatchWindowContext> GetMatchWindowContextAsync(DateTime? time = null);
}

public record MatchWindowContext(
    DateTime Timestamp,
    bool IsActive,
    double DemandMultiplier,
    DateTime? EndTime);

public class MatchWindowService : IMatchWindowService
{
    private readonly IEventsService _eventsService;

    public MatchWindowService(IEventsService eventsService)
    {
        _eventsService = eventsService;
    }

    public async Task<MatchWindowContext> GetMatchWindowContextAsync(DateTime? time = null)
    {
        var now = time ?? DateTime.UtcNow;
        var eventsResponse = await _eventsService.GetActiveEventsAsync(time);
        var demandResponse = await _eventsService.GetDemandMultiplierAsync(time);

        var isActive = eventsResponse?.InMatchWindow ?? false;
        var demandMultiplier = demandResponse?.Multiplier ?? 1.0;

        DateTime? matchWindowEnd = null;
        if (eventsResponse?.ActiveEvents?.Any() == true)
        {
            matchWindowEnd = eventsResponse.ActiveEvents
                .Max(e => now.AddMinutes(e.MinutesRemaining + 30));
        }

        return new MatchWindowContext(now, isActive, demandMultiplier, matchWindowEnd);
    }
}
