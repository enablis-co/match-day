namespace Staffing.Models;

public class SignalsResponse
{
    public string PubId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public SignalsDetail Signals { get; set; } = new();
}

public class SignalsDetail
{
    public double DemandMultiplier { get; set; }

    public bool MatchWindowActive { get; set; }

    public string MatchDescription { get; set; } = string.Empty;

    public string StockPressure { get; set; } = "NONE";

    public int StockAlerts { get; set; }

    public double HistoricalAverage { get; set; }
}
