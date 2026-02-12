using Stock.Services;

namespace Stock.Tests.Services;

public class StockCalculationServiceTests
{
    private readonly IStockCalculationService _service = new StockCalculationService();

    [Fact]
    public void CalculateAdjustedRate_ReturnsCorrectProduct()
    {
        var result = _service.CalculateAdjustedRate(10, 2.5);

        Assert.Equal(25, result);
    }

    [Fact]
    public void CalculateAdjustedRate_HandlesZeroMultiplier()
    {
        var result = _service.CalculateAdjustedRate(10, 0);

        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateAdjustedRate_HandlesZeroBaseRate()
    {
        var result = _service.CalculateAdjustedRate(0, 2.5);

        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateHoursRemaining_ReturnsCorrectDivision()
    {
        var result = _service.CalculateHoursRemaining(100, 10);

        Assert.Equal(10.0, result);
    }

    [Fact]
    public void CalculateHoursRemaining_ReturnsNull_WhenAdjustedRateIsZero()
    {
        var result = _service.CalculateHoursRemaining(100, 0);

        Assert.Null(result);
    }

    [Fact]
    public void CalculateHoursRemaining_HandlesPartialHours()
    {
        var result = _service.CalculateHoursRemaining(50, 8);

        Assert.Equal(6.25, result);
    }

    [Fact]
    public void CalculateDepletionTime_ReturnsCorrectFutureTime()
    {
        var beforeTime = DateTime.UtcNow;
        var result = _service.CalculateDepletionTime(5.0);

        Assert.NotNull(result);
        Assert.True(result > beforeTime.AddHours(4.5));
        Assert.True(result < beforeTime.AddHours(5.5));
    }

    [Fact]
    public void CalculateDepletionTime_ReturnsNull_WhenHoursRemainingIsNull()
    {
        var result = _service.CalculateDepletionTime(null);

        Assert.Null(result);
    }

    [Fact]
    public void WillDeplete_ReturnsTrue_WhenBelowThreshold()
    {
        var result = _service.WillDeplete(3.0, 4.0);

        Assert.True(result);
    }

    [Fact]
    public void WillDeplete_ReturnsTrue_WhenEqualToThreshold()
    {
        var result = _service.WillDeplete(4.0, 4.0);

        Assert.True(result);
    }

    [Fact]
    public void WillDeplete_ReturnsFalse_WhenAboveThreshold()
    {
        var result = _service.WillDeplete(5.0, 4.0);

        Assert.False(result);
    }

    [Fact]
    public void WillDeplete_ReturnsFalse_WhenHoursRemainingIsNull()
    {
        var result = _service.WillDeplete(null, 4.0);

        Assert.False(result);
    }

    [Theory]
    [InlineData(100, 10, 1.0, 10.0)]
    [InlineData(50, 8, 2.0, 3.125)]
    [InlineData(20, 5, 1.5, 2.666666666666667)]
    public void IntegrationTest_CalculateFullChain(double currentLevel, double baseRate, double multiplier, double expectedHours)
    {
        var adjustedRate = _service.CalculateAdjustedRate(baseRate, multiplier);
        var hoursRemaining = _service.CalculateHoursRemaining(currentLevel, adjustedRate);

        Assert.NotNull(hoursRemaining);
        Assert.Equal(expectedHours, hoursRemaining.Value, precision: 10);
    }

    [Fact]
    public void IntegrationTest_FullWorkflow_WithDepletion()
    {
        var adjustedRate = _service.CalculateAdjustedRate(10, 2.0);
        var hoursRemaining = _service.CalculateHoursRemaining(30, adjustedRate);
        var depletionTime = _service.CalculateDepletionTime(hoursRemaining);
        var willDeplete = _service.WillDeplete(hoursRemaining, 4.0);

        Assert.Equal(20, adjustedRate);
        Assert.Equal(1.5, hoursRemaining);
        Assert.NotNull(depletionTime);
        Assert.True(willDeplete);
    }

    [Fact]
    public void IntegrationTest_FullWorkflow_NoDepletion()
    {
        var adjustedRate = _service.CalculateAdjustedRate(5, 1.0);
        var hoursRemaining = _service.CalculateHoursRemaining(50, adjustedRate);
        var depletionTime = _service.CalculateDepletionTime(hoursRemaining);
        var willDeplete = _service.WillDeplete(hoursRemaining, 4.0);

        Assert.Equal(5, adjustedRate);
        Assert.Equal(10.0, hoursRemaining);
        Assert.NotNull(depletionTime);
        Assert.False(willDeplete);
    }
}
