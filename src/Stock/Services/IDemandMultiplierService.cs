using Stock.Models;

namespace Stock.Services;

public interface IDemandMultiplierService
{
    Task<DemandMultiplierResponse> GetDemandMultiplierAsync();
}