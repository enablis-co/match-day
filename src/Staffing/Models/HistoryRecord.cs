namespace Staffing.Models;

public class HistoryRecord
{
    public string Date { get; set; } = string.Empty;

    public string Event { get; set; } = string.Empty;

    public int Recommended { get; set; }

    public string Outcome { get; set; } = "PENDING";

    public string Feedback { get; set; } = string.Empty;
}

public class HistoryResponse
{
    public string PubId { get; set; } = string.Empty;

    public List<HistoryRecord> Recommendations { get; set; } = [];
}
