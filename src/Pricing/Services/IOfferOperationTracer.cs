using System.Diagnostics;

namespace Pricing.Services;

public interface IOfferOperationTracer : IDisposable
{
    void StartOperation(string operationName);
    void LogStep(string stepName, Dictionary<string, object>? properties = null);
    void EndOperation(bool success = true, string? errorMessage = null);
}
