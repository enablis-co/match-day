using Surge.Clients.Dtos;

namespace Surge.Clients;

public interface IEventsClient
{
    Task<TodayEventsResponse?> GetTodayEventsAsync();
    Task<DemandMultiplierResponse?> GetDemandMultiplierAsync();
}
