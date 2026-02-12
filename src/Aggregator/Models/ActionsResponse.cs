namespace Aggregator.Models;

public record ActionsResponse(
    string PubId,
    DateTime Timestamp,
    List<PubAction> Actions
);
