namespace Staffing.Clients.Dtos;

public class ActiveEventsResponse
{
    public DateTime Timestamp { get; set; }

    public List<ActiveEvent> ActiveEvents { get; set; } = [];

    public bool InMatchWindow { get; set; }
}

public class ActiveEvent
{
    public string EventId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int MinutesRemaining { get; set; }

    public double DemandMultiplier { get; set; }
}

public class DemandMultiplierResponse
{
    public DateTime Timestamp { get; set; }

    public double Multiplier { get; set; }

    public string Reason { get; set; } = string.Empty;
}
