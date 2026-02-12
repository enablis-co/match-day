using Aggregator.Models;

namespace Aggregator.Services;

public interface IAggregatorService
{
    Task<PubStatusResponse> GetStatusAsync(string pubId, DateTime? time);

    Task<SummaryResponse> GetSummaryAsync(string pubId, DateTime? time);

    Task<ActionsResponse> GetActionsAsync(string pubId, DateTime? time);

    Task<HealthResponse> GetHealthAsync();
}
