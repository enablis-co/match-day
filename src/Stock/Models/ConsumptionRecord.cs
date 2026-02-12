namespace Stock.Models;

public class ConsumptionRecord
{
    public int Id { get; set; }
    public string PubId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public double Amount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}