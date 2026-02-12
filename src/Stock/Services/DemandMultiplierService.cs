using System.Text.Json;
using Stock.Models;

namespace Stock.Services;

public class DemandMultiplierService : IDemandMultiplierService
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(3);

    private readonly HttpClient _httpClient;

    public DemandMultiplierService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DemandMultiplierResponse> GetDemandMultiplierAsync()
    {
        using var cts = new CancellationTokenSource(RequestTimeout);

        try
        {
            var response = await _httpClient.GetAsync("/events/demand-multiplier", cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cts.Token);
                var jsonDoc = JsonDocument.Parse(content);

                if (jsonDoc.RootElement.TryGetProperty("multiplier", out var multiplierElement))
                {
                    return new DemandMultiplierResponse(
                        multiplierElement.GetDouble(),
                        IsDefault: false,
                        Source: "EventsService"
                    );
                }
            }

            return new DemandMultiplierResponse(1.0, IsDefault: true, Source: "DefaultFallback_NonSuccessStatus");
        }
        catch (OperationCanceledException)
        {
            return new DemandMultiplierResponse(1.0, IsDefault: true, Source: "DefaultFallback_Timeout");
        }
        catch
        {
            return new DemandMultiplierResponse(1.0, IsDefault: true, Source: "DefaultFallback_Error");
        }
    }
}