using Aggregator.Models;

namespace Aggregator.Clients;

public interface IStaffingClient
{
    Task<StaffingSummary> GetRecommendationAsync(string pubId, DateTime? time);
}
