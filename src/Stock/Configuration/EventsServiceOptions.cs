namespace Stock.Configuration;

public class EventsServiceOptions
{
    public const string SectionName = "EventsService";

    public string BaseUrl { get; set; } = "http://localhost:8080";
    public int TimeoutSeconds { get; set; } = 3;
    public int MaxRetries { get; set; } = 0;

    public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds);
}
