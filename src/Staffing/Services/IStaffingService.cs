using Staffing.Models;

namespace Staffing.Services;

public interface IStaffingService
{
    Task<StaffingRecommendation> GetRecommendationAsync(string pubId, DateTime? time = null);
    Task<SignalsResponse> GetSignalsAsync(string pubId, DateTime? time = null);
    HistoryResponse GetHistory(string pubId, int days = 7);
}
