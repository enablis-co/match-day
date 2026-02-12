using System.Diagnostics;

namespace Pricing.Services;

public class OfferOperationTracer : IOfferOperationTracer
{
    private readonly ILogger<OfferOperationTracer> _logger;
    private Activity? _activity;
    private readonly Stopwatch _stopwatch;
    private string _operationName = "";

    public OfferOperationTracer(ILogger<OfferOperationTracer> logger)
    {
        _logger = logger;
        _stopwatch = new Stopwatch();
    }

    public void StartOperation(string operationName)
    {
        _operationName = operationName;
        _stopwatch.Restart();

        _activity = new Activity(operationName).Start();
        _activity?.SetTag("operation.type", operationName);

        _logger.LogInformation(
            "Starting operation: {OperationName}",
            operationName);
    }

    public void LogStep(string stepName, Dictionary<string, object>? properties = null)
    {
        var elapsed = _stopwatch.ElapsedMilliseconds;

        var tags = new Dictionary<string, object>
        {
            { "step.name", stepName },
            { "step.elapsed_ms", elapsed }
        };

        if (properties != null)
        {
            foreach (var prop in properties)
            {
                tags[$"step.{prop.Key}"] = prop.Value;
            }
        }

        _activity?.AddEvent(new ActivityEvent(stepName, tags: new ActivityTagsCollection(tags)));

        _logger.LogDebug(
            "Operation step: {OperationName} -> {StepName} ({ElapsedMs}ms)",
            _operationName,
            stepName,
            elapsed);
    }

    public void EndOperation(bool success = true, string? errorMessage = null)
    {
        var elapsed = _stopwatch.ElapsedMilliseconds;

        _activity?.SetTag("operation.success", success);
        _activity?.SetTag("operation.elapsed_ms", elapsed);

        if (!success && !string.IsNullOrEmpty(errorMessage))
        {
            _activity?.SetTag("operation.error", errorMessage);
        }

        _activity?.Dispose();

        var logLevel = success ? LogLevel.Information : LogLevel.Error;
        _logger.Log(
            logLevel,
            "Completed operation: {OperationName} - Success: {Success}, Elapsed: {ElapsedMs}ms {ErrorMessage}",
            _operationName,
            success,
            elapsed,
            errorMessage ?? "");

        _stopwatch.Stop();
    }

    public void Dispose()
    {
        _activity?.Dispose();
    }
}
