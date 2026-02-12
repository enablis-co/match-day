namespace Stock.Models;

public class StockLevel
{
    public int Id { get; set; }
    public string PubId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public double CurrentLevel { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    public Product? Product { get; set; }
    
    public string Status => CurrentLevel switch
    {
        <= 10 => "CRITICAL",
        <= 30 => "LOW",
        _ => "OK"
    };
}