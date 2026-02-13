using Aggregator.Models;

namespace Aggregator.Clients;

public interface ISurgeClient
{
    Task<SurgeSummary?> GetPeakAsync(string pubId);

    Task<SurgeForecast?> GetForecastAsync(string pubId, int hours = 12);
}
