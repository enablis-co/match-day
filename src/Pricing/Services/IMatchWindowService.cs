namespace Pricing.Services;

public interface IMatchWindowService
{
    Task<MatchWindowContext> GetMatchWindowContextAsync(DateTime? time = null);
}
