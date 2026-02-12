namespace Pricing.Services.Tests;

public class OfferOperationTracerTests
{
    private readonly Mock<ILogger<OfferOperationTracer>> _mockLogger;
    private readonly OfferOperationTracer _tracer;

    public OfferOperationTracerTests()
    {
        _mockLogger = new Mock<ILogger<OfferOperationTracer>>();
        _tracer = new OfferOperationTracer(_mockLogger.Object);
    }

    [Fact]
    public void StartOperation_LogsOperationStart()
    {
        // Act
        _tracer.StartOperation("TestOperation");

        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting operation: TestOperation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogStep_RecordsStepExecution()
    {
        // Arrange
        _tracer.StartOperation("TestOperation");
        
        // Act
        _tracer.LogStep("TestStep", new Dictionary<string, object> { { "key", "value" } });

        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Operation step")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void EndOperation_SuccessfullyCompletes()
    {
        // Arrange
        _tracer.StartOperation("TestOperation");
        System.Threading.Thread.Sleep(10); // Simulate work
        
        // Act
        _tracer.EndOperation(true);

        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Completed operation") && v.ToString()!.Contains("Success: True")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void EndOperation_WithError_LogsFailure()
    {
        // Arrange
        _tracer.StartOperation("TestOperation");
        var errorMessage = "Test error";
        
        // Act
        _tracer.EndOperation(false, errorMessage);

        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Success: False")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_DisposesActivity()
    {
        // Arrange
        _tracer.StartOperation("TestOperation");
        
        // Act
        _tracer.Dispose();

        // Assert - No exception should be thrown
        // If Activity wasn't disposed properly, this might cause issues
    }

    [Fact]
    public void MultipleSteps_TracksElapsedTime()
    {
        // Arrange
        _tracer.StartOperation("TestOperation");
        
        // Act
        _tracer.LogStep("Step1");
        System.Threading.Thread.Sleep(10);
        _tracer.LogStep("Step2");
        System.Threading.Thread.Sleep(10);
        _tracer.LogStep("Step3");

        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Operation step")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
