namespace Staffing.Clients.Dtos;

public class StockAlertsResponse
{
    public string PubId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public string OverallPressure { get; set; } = "NONE";

    public int AlertCount { get; set; }

    public List<StockAlert> Alerts { get; set; } = [];
}

public class StockAlert
{
    public string ProductId { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string Severity { get; set; } = "LOW";

    public string Message { get; set; } = string.Empty;
}
