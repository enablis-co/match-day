using Pricing.Models.Dtos;

namespace Pricing.Services;

public interface IEventsService
{
    Task<EventsActiveResponse?> GetActiveEventsAsync(DateTime? time = null);
    Task<DemandMultiplierResponse?> GetDemandMultiplierAsync(DateTime? time = null);
}
