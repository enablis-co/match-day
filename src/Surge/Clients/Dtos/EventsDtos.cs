namespace Surge.Clients.Dtos;

public class TodayEventsResponse
{
    public string Date { get; set; } = string.Empty;

    public List<EventDto> Events { get; set; } = [];
}

public class EventDto
{
    public string EventId { get; set; } = string.Empty;

    public string Sport { get; set; } = string.Empty;

    public string Competition { get; set; } = string.Empty;

    public string HomeTeam { get; set; } = string.Empty;

    public string AwayTeam { get; set; } = string.Empty;

    public DateTime Kickoff { get; set; }

    public DateTime ExpectedEnd { get; set; }

    public double DemandMultiplier { get; set; }
}

public class DemandMultiplierResponse
{
    public DateTime Timestamp { get; set; }

    public double Multiplier { get; set; }

    public string Reason { get; set; } = string.Empty;
}
