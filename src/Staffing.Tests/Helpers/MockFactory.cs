using Moq;
using Staffing.Clients;
using Staffing.Clients.Dtos;
using Microsoft.Extensions.Logging;

namespace Staffing.Tests.Helpers;

public static class MockFactory
{
    public static Mock<IEventsClient> CreateEventsClient(
        double demandMultiplier = 1.0,
        string description = "England vs France",
        bool matchActive = true,
        bool returnsNull = false)
    {
        var mock = new Mock<IEventsClient>();

        if (returnsNull)
        {
            mock.Setup(x => x.GetActiveEventsAsync(It.IsAny<DateTime?>()))
                .ReturnsAsync((ActiveEventsResponse?)null);
            mock.Setup(x => x.GetDemandMultiplierAsync(It.IsAny<DateTime?>()))
                .ReturnsAsync((DemandMultiplierResponse?)null);
            return mock;
        }

        var activeEvents = matchActive
            ? new List<ActiveEvent>
            {
                new()
                {
                    EventId = "EVT-001",
                    Description = description,
                    MinutesRemaining = 60,
                    DemandMultiplier = demandMultiplier
                }
            }
            : new List<ActiveEvent>();

        mock.Setup(x => x.GetActiveEventsAsync(It.IsAny<DateTime?>()))
            .ReturnsAsync(new ActiveEventsResponse
            {
                Timestamp = DateTime.UtcNow,
                ActiveEvents = activeEvents,
                InMatchWindow = matchActive
            });

        return mock;
    }

    public static Mock<IStockClient> CreateStockClient(
        string overallPressure = "NONE",
        int alertCount = 0,
        string pubId = "PUB-001",
        bool returnsNull = false,
        bool returnsStub = false)
    {
        var mock = new Mock<IStockClient>();

        if (returnsNull)
        {
            mock.Setup(x => x.GetStockAlertsAsync(It.IsAny<string>()))
                .ReturnsAsync((StockAlertsResponse?)null);
            return mock;
        }

        if (returnsStub)
        {
            // Simulates the stub response from the unimplemented Stock Service
            mock.Setup(x => x.GetStockAlertsAsync(It.IsAny<string>()))
                .ReturnsAsync(new StockAlertsResponse
                {
                    PubId = "",
                    Timestamp = default,
                    OverallPressure = "NONE",
                    AlertCount = 0,
                    Alerts = []
                });
            return mock;
        }

        var alerts = new List<StockAlert>();
        for (var i = 0; i < alertCount; i++)
        {
            alerts.Add(new StockAlert
            {
                ProductId = $"PROD-{i + 1:D3}",
                ProductName = $"Product {i + 1}",
                Severity = overallPressure,
                Message = $"Alert {i + 1}"
            });
        }

        mock.Setup(x => x.GetStockAlertsAsync(It.IsAny<string>()))
            .ReturnsAsync(new StockAlertsResponse
            {
                PubId = pubId,
                Timestamp = DateTime.UtcNow,
                OverallPressure = overallPressure,
                AlertCount = alertCount,
                Alerts = alerts
            });

        return mock;
    }

    public static ILogger<T> CreateLogger<T>() => Mock.Of<ILogger<T>>();
}
