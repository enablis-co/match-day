using Pricing.Models;

namespace Pricing.Data;

public class OfferRepository
{
    private readonly List<Offer> _offers;

    public OfferRepository()
    {
        _offers = new List<Offer>
        {
            new()
            {
                OfferId = "OFFER-001",
                Name = "Happy Hour",
                Description = "50% off selected pints",
                DiscountType = DiscountType.PERCENTAGE,
                DiscountValue = 50,
                ApplicableProducts = ["PINT_LAGER", "PINT_ALE", "PINT_STELLA"],
                Schedule = new Schedule
                {
                    Days = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
                    StartTime = new TimeOnly(16, 0),
                    EndTime = new TimeOnly(18, 0)
                },
                MatchDayRule = MatchDayRule.END_EARLY
            },
            new()
            {
                OfferId = "OFFER-002",
                Name = "2-for-1 Cocktails",
                Description = "Buy one cocktail, get one free",
                DiscountType = DiscountType.BUY_ONE_GET_ONE,
                DiscountValue = 100,
                ApplicableProducts = ["COCKTAIL_MOJITO", "COCKTAIL_MARGARITA", "COCKTAIL_ESPRESSO_MARTINI"],
                Schedule = new Schedule
                {
                    Days = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday],
                    StartTime = new TimeOnly(12, 0),
                    EndTime = new TimeOnly(22, 0)
                },
                MatchDayRule = MatchDayRule.SUSPEND
            },
            new()
            {
                OfferId = "OFFER-003",
                Name = "Weekend Special",
                Description = "£1 off all pints",
                DiscountType = DiscountType.FIXED_AMOUNT,
                DiscountValue = 1.00m,
                ApplicableProducts = ["PINT_LAGER", "PINT_ALE", "PINT_STELLA", "PINT_GUINNESS"],
                Schedule = new Schedule
                {
                    Days = [DayOfWeek.Saturday, DayOfWeek.Sunday],
                    StartTime = new TimeOnly(12, 0),
                    EndTime = new TimeOnly(20, 0)
                },
                MatchDayRule = MatchDayRule.CONTINUE
            }
        };
    }

    public IReadOnlyList<Offer> GetAll() => _offers.AsReadOnly();

    public Offer? GetById(string offerId) => 
        _offers.FirstOrDefault(o => o.OfferId.Equals(offerId, StringComparison.OrdinalIgnoreCase));
}
