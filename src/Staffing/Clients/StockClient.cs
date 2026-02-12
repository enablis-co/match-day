using Staffing.Clients.Dtos;

namespace Staffing.Clients;

public interface IStockClient
{
    Task<StockAlertsResponse?> GetStockAlertsAsync(string pubId);
}

public class StockClient : IStockClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StockClient> _logger;

    public StockClient(HttpClient httpClient, ILogger<StockClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<StockAlertsResponse?> GetStockAlertsAsync(string pubId)
    {
        try
        {
            var url = $"/stock/alerts?pubId={pubId}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<StockAlertsResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch stock alerts from Stock Service for pub {PubId}", pubId);
            return null;
        }
    }
}
