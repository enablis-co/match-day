namespace Pricing.Services.Tests;

public class OfferEvaluationServiceTests
{
    private readonly Mock<IOfferRepository> _mockOfferRepository;
    private readonly OfferEvaluationService _service;

    public OfferEvaluationServiceTests()
    {
        _mockOfferRepository = new Mock<IOfferRepository>();
        _service = new OfferEvaluationService(_mockOfferRepository.Object);
    }

    #region IsWithinSchedule Tests

    [Fact]
    public void IsWithinSchedule_WithinBusinessHours_ReturnsTrue()
    {
        // Arrange
        var offer = new Offer
        {
            OfferId = "OFFER-001",
            Schedule = new Schedule
            {
                Days = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday],
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(18, 0)
            }
        };
        var wednesday5pm = new DateTime(2024, 1, 10, 17, 0, 0); // Wednesday 5 PM

        // Act
        var result = _service.IsWithinSchedule(offer, wednesday5pm);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsWithinSchedule_OutsideScheduledHours_ReturnsFalse()
    {
        // Arrange
        var offer = new Offer
        {
            OfferId = "OFFER-001",
            Schedule = new Schedule
            {
                Days = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday],
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(18, 0)
            }
        };
        var wednesday7pm = new DateTime(2024, 1, 10, 19, 0, 0); // Wednesday 7 PM (after 6 PM)

        // Act
        var result = _service.IsWithinSchedule(offer, wednesday7pm);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsWithinSchedule_OnNonScheduledDay_ReturnsFalse()
    {
        // Arrange
        var offer = new Offer
        {
            OfferId = "OFFER-001",
            Schedule = new Schedule
            {
                Days = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday],
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(18, 0)
            }
        };
        var saturday5pm = new DateTime(2024, 1, 13, 17, 0, 0); // Saturday 5 PM

        // Act
        var result = _service.IsWithinSchedule(offer, saturday5pm);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsWithinSchedule_OnScheduleBoundaryStart_ReturnsTrue()
    {
        // Arrange
        var offer = new Offer
        {
            OfferId = "OFFER-001",
            Schedule = new Schedule
            {
                Days = [DayOfWeek.Friday],
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(18, 0)
            }
        };
        var friday4pm = new DateTime(2024, 1, 12, 16, 0, 0); // Friday 4 PM (exactly start)

        // Act
        var result = _service.IsWithinSchedule(offer, friday4pm);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region EvaluateOffer Tests

    [Fact]
    public void EvaluateOffer_NotWithinSchedule_ReturnsInactive()
    {
        // Arrange
        var offer = new Offer
        {
            OfferId = "OFFER-001",
            Schedule = new Schedule
            {
                Days = [DayOfWeek.Monday],
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(18, 0)
            }
        };
        var friday5pm = new DateTime(2024, 1, 12, 17, 0, 0); // Friday (not Monday)

        // Act
        var result = _service.EvaluateOffer(offer, friday5pm, false, 1.0, null);

        // Assert
        Assert.Equal(OfferStatus.INACTIVE, result.Status);
    }

    [Fact]
    public void EvaluateOffer_WithinScheduleNoMatchDay_ReturnsActive()
    {
        // Arrange
        var offer = new Offer
        {
            OfferId = "OFFER-001",
            Schedule = new Schedule
            {
                Days = [DayOfWeek.Friday],
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(18, 0)
            },
            MatchDayRule = MatchDayRule.CONTINUE
        };
        var friday5pm = new DateTime(2024, 1, 12, 17, 0, 0); // Friday 5 PM

        // Act
        var result = _service.EvaluateOffer(offer, friday5pm, false, 1.0, null);

        // Assert
        Assert.Equal(OfferStatus.ACTIVE, result.Status);
    }

    [Fact]
    public void EvaluateOffer_MatchDaySuspendRule_ReturnsSuspended()
    {
        // Arrange
        var offer = new Offer
        {
            OfferId = "OFFER-001",
            Schedule = new Schedule
            {
                Days = [DayOfWeek.Friday],
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(18, 0)
            },
            MatchDayRule = MatchDayRule.SUSPEND
        };
        var friday5pm = new DateTime(2024, 1, 12, 17, 0, 0);
        var matchWindowEnd = friday5pm.AddHours(2);

        // Act
        var result = _service.EvaluateOffer(offer, friday5pm, true, 1.0, matchWindowEnd);

        // Assert
        Assert.Equal(OfferStatus.SUSPENDED, result.Status);
        Assert.Equal(matchWindowEnd, result.ResumesAt);
    }

    [Fact]
    public void EvaluateOffer_MatchDayEndEarlyRule_ReturnsEndedEarly()
    {
        // Arrange
        var offer = new Offer
        {
            OfferId = "OFFER-001",
            Schedule = new Schedule
            {
                Days = [DayOfWeek.Friday],
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(18, 0)
            },
            MatchDayRule = MatchDayRule.END_EARLY
        };
        var friday5pm = new DateTime(2024, 1, 12, 17, 0, 0);

        // Act
        var result = _service.EvaluateOffer(offer, friday5pm, true, 1.0, null);

        // Assert
        Assert.Equal(OfferStatus.ENDED_EARLY, result.Status);
    }

    [Fact]
    public void EvaluateOffer_HighDemandMultiplier_SuspendsOffer()
    {
        // Arrange
        var offer = new Offer
        {
            OfferId = "OFFER-001",
            Schedule = new Schedule
            {
                Days = [DayOfWeek.Friday],
                StartTime = new TimeOnly(16, 0),
                EndTime = new TimeOnly(18, 0)
            },
            MatchDayRule = MatchDayRule.CONTINUE
        };
        var friday5pm = new DateTime(2024, 1, 12, 17, 0, 0);
        var matchWindowEnd = friday5pm.AddHours(2);

        // Act - Demand multiplier > 1.5
        var result = _service.EvaluateOffer(offer, friday5pm, true, 2.0, matchWindowEnd);

        // Assert
        Assert.Equal(OfferStatus.SUSPENDED, result.Status);
        Assert.Equal("Demand multiplier too high", result.Reason);
    }

    #endregion

    #region EvaluateAllOffers Tests

    [Fact]
    public void EvaluateAllOffers_NoOffers_ReturnsEmptyList()
    {
        // Arrange
        _mockOfferRepository.Setup(r => r.GetAll())
            .Returns(new List<Offer>().AsReadOnly());

        // Act
        var result = _service.EvaluateAllOffers(DateTime.UtcNow, false, 1.0, null);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void EvaluateAllOffers_MultipleOffers_EvaluatesAll()
    {
        // Arrange
        var offers = new List<Offer>
        {
            new()
            {
                OfferId = "OFFER-001",
                Schedule = new Schedule
                {
                    Days = [DayOfWeek.Friday],
                    StartTime = new TimeOnly(16, 0),
                    EndTime = new TimeOnly(18, 0)
                },
                MatchDayRule = MatchDayRule.CONTINUE
            },
            new()
            {
                OfferId = "OFFER-002",
                Schedule = new Schedule
                {
                    Days = [DayOfWeek.Monday],
                    StartTime = new TimeOnly(12, 0),
                    EndTime = new TimeOnly(14, 0)
                },
                MatchDayRule = MatchDayRule.CONTINUE
            }
        };
        _mockOfferRepository.Setup(r => r.GetAll())
            .Returns(offers.AsReadOnly());

        var friday5pm = new DateTime(2024, 1, 12, 17, 0, 0);

        // Act
        var result = _service.EvaluateAllOffers(friday5pm, false, 1.0, null);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Offer.OfferId == "OFFER-001" && e.Status == OfferStatus.ACTIVE);
        Assert.Contains(result, e => e.Offer.OfferId == "OFFER-002" && e.Status == OfferStatus.INACTIVE);
    }

    #endregion
}
