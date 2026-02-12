namespace Pricing.Services.Tests;

public class MatchWindowServiceTests
{
    private readonly Mock<IEventsService> _mockEventsService;
    private readonly MatchWindowService _service;

    public MatchWindowServiceTests()
    {
        _mockEventsService = new Mock<IEventsService>();
        _service = new MatchWindowService(_mockEventsService.Object);
    }

    [Fact]
    public async Task GetMatchWindowContextAsync_NoActiveEvents_ReturnsInactiveContext()
    {
        // Arrange
        var now = DateTime.UtcNow;
        _mockEventsService.Setup(s => s.GetActiveEventsAsync(It.IsAny<DateTime?>()))
            .ReturnsAsync(new Models.Dtos.EventsActiveResponse
            {
                Timestamp = now,
                ActiveEvents = new List<Models.Dtos.ActiveEvent>(),
                InMatchWindow = false
            });
        
        _mockEventsService.Setup(s => s.GetDemandMultiplierAsync(It.IsAny<DateTime?>()))
            .ReturnsAsync(new Models.Dtos.DemandMultiplierResponse
            {
                Timestamp = now,
                Multiplier = 1.0,
                Reason = "No active events"
            });

        // Act
        var result = await _service.GetMatchWindowContextAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        Assert.Equal(1.0, result.DemandMultiplier);
        Assert.Null(result.EndTime);
    }

    [Fact]
    public async Task GetMatchWindowContextAsync_WithActiveEvents_CalculatesEndTime()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var activeEvent = new Models.Dtos.ActiveEvent
        {
            EventId = "EVT-001",
            Description = "Match A",
            MinutesRemaining = 45,
            DemandMultiplier = 2.0
        };
        
        _mockEventsService.Setup(s => s.GetActiveEventsAsync(It.IsAny<DateTime?>()))
            .ReturnsAsync(new Models.Dtos.EventsActiveResponse
            {
                Timestamp = now,
                ActiveEvents = new List<Models.Dtos.ActiveEvent> { activeEvent },
                InMatchWindow = true
            });
        
        _mockEventsService.Setup(s => s.GetDemandMultiplierAsync(It.IsAny<DateTime?>()))
            .ReturnsAsync(new Models.Dtos.DemandMultiplierResponse
            {
                Timestamp = now,
                Multiplier = 2.0,
                Reason = "Match in progress"
            });

        // Act
        var result = await _service.GetMatchWindowContextAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsActive);
        Assert.Equal(2.0, result.DemandMultiplier);
        Assert.NotNull(result.EndTime);
        // EndTime should be 30 minutes after the end of the match
        var expectedEnd = now.AddMinutes(45 + 30);
        Assert.True(Math.Abs((result.EndTime.Value - expectedEnd).TotalSeconds) < 1);
    }

    [Fact]
    public async Task GetMatchWindowContextAsync_WithSpecificTime_UsesProvidedTime()
    {
        // Arrange
        var specificTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        
        _mockEventsService.Setup(s => s.GetActiveEventsAsync(specificTime))
            .ReturnsAsync(new Models.Dtos.EventsActiveResponse
            {
                Timestamp = specificTime,
                ActiveEvents = new List<Models.Dtos.ActiveEvent>(),
                InMatchWindow = false
            });
        
        _mockEventsService.Setup(s => s.GetDemandMultiplierAsync(specificTime))
            .ReturnsAsync(new Models.Dtos.DemandMultiplierResponse
            {
                Timestamp = specificTime,
                Multiplier = 1.0,
                Reason = "No active events"
            });

        // Act
        var result = await _service.GetMatchWindowContextAsync(specificTime);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(specificTime, result.Timestamp);
        _mockEventsService.Verify(s => s.GetActiveEventsAsync(specificTime), Times.Once);
        _mockEventsService.Verify(s => s.GetDemandMultiplierAsync(specificTime), Times.Once);
    }

    [Fact]
    public async Task GetMatchWindowContextAsync_EventsServiceReturnsNull_HandlesGracefully()
    {
        // Arrange
        var now = DateTime.UtcNow;
        _mockEventsService.Setup(s => s.GetActiveEventsAsync(It.IsAny<DateTime?>()))
            .ReturnsAsync((Models.Dtos.EventsActiveResponse?)null);
        
        _mockEventsService.Setup(s => s.GetDemandMultiplierAsync(It.IsAny<DateTime?>()))
            .ReturnsAsync((Models.Dtos.DemandMultiplierResponse?)null);

        // Act
        var result = await _service.GetMatchWindowContextAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        Assert.Equal(1.0, result.DemandMultiplier);
    }
}
