namespace Pricing.Models;

public class Offer
{
    public string OfferId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public List<string> ApplicableProducts { get; set; } = [];
    public Schedule Schedule { get; set; } = new();
    public MatchDayRule MatchDayRule { get; set; }
}
