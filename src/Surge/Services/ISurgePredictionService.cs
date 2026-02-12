using Surge.Models;

namespace Surge.Services;

public interface ISurgePredictionService
{
    Task<ForecastResponse> GetForecastAsync(string pubId, int hours);
    Task<PeakResponse> GetPeakAsync(string pubId, int hours = 8);
}
