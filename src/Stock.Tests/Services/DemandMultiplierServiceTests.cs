using System.Net;
using Stock.Services;

namespace Stock.Tests.Services;

public class DemandMultiplierServiceTests
{
    private static DemandMultiplierService CreateService(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };
        return new DemandMultiplierService(client);
    }

    [Fact]
    public async Task GetDemandMultiplierAsync_ReturnsMultiplier_WhenServiceRespondsSuccessfully()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, """{"multiplier": 2.5}""");
        var service = CreateService(handler);

        var result = await service.GetDemandMultiplierAsync();

        Assert.Equal(2.5, result.Multiplier);
        Assert.False(result.IsDefault);
        Assert.Equal("EventsService", result.Source);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(3.75)]
    public async Task GetDemandMultiplierAsync_ReturnsExactValue_ForVariousMultipliers(double expected)
    {
        var handler = new FakeHandler(HttpStatusCode.OK, $$$"""{"multiplier": {{{expected}}}}""");
        var service = CreateService(handler);

        var result = await service.GetDemandMultiplierAsync();

        Assert.Equal(expected, result.Multiplier);
        Assert.False(result.IsDefault);
    }

    [Fact]
    public async Task GetDemandMultiplierAsync_ReturnsFallback_WhenResponseIsMissingMultiplierProperty()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, """{"other": 2.0}""");
        var service = CreateService(handler);

        var result = await service.GetDemandMultiplierAsync();

        Assert.Equal(1.0, result.Multiplier);
        Assert.True(result.IsDefault);
        Assert.Equal("DefaultFallback_NonSuccessStatus", result.Source);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task GetDemandMultiplierAsync_ReturnsFallback_WhenServiceReturnsNonSuccessStatus(HttpStatusCode statusCode)
    {
        var handler = new FakeHandler(statusCode, """{"multiplier": 2.0}""");
        var service = CreateService(handler);

        var result = await service.GetDemandMultiplierAsync();

        Assert.Equal(1.0, result.Multiplier);
        Assert.True(result.IsDefault);
        Assert.Equal("DefaultFallback_NonSuccessStatus", result.Source);
    }

    [Fact]
    public async Task GetDemandMultiplierAsync_ReturnsFallback_WhenServiceThrowsException()
    {
        var handler = new ThrowingHandler(new HttpRequestException("Connection refused"));
        var service = CreateService(handler);

        var result = await service.GetDemandMultiplierAsync();

        Assert.Equal(1.0, result.Multiplier);
        Assert.True(result.IsDefault);
        Assert.Equal("DefaultFallback_Error", result.Source);
    }

    [Fact]
    public async Task GetDemandMultiplierAsync_ReturnsFallback_WhenRequestTimesOut()
    {
        var handler = new DelayedHandler(TimeSpan.FromSeconds(10));
        var service = CreateService(handler);

        var result = await service.GetDemandMultiplierAsync();

        Assert.Equal(1.0, result.Multiplier);
        Assert.True(result.IsDefault);
        Assert.Equal("DefaultFallback_Timeout", result.Source);
    }

    [Fact]
    public async Task GetDemandMultiplierAsync_ReturnsFallback_WhenResponseBodyIsInvalidJson()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, "not json");
        var service = CreateService(handler);

        var result = await service.GetDemandMultiplierAsync();

        Assert.Equal(1.0, result.Multiplier);
        Assert.True(result.IsDefault);
        Assert.Equal("DefaultFallback_Error", result.Source);
    }

    // --- Test helpers ---

    private class FakeHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            };
            return Task.FromResult(response);
        }
    }

    private class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw exception;
        }
    }

    private class DelayedHandler(TimeSpan delay) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"multiplier": 2.0}""")
            };
        }
    }
}