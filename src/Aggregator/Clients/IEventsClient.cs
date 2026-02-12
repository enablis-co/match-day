using Aggregator.Models;

namespace Aggregator.Clients;

public interface IEventsClient
{
    Task<EventSummary> GetActiveEventsAsync(DateTime? time);
}
