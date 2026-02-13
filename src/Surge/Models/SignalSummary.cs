namespace Surge.Models;

public class SignalSummary
{
    public bool EventsAvailable { get; set; }

    public bool WeatherAvailable { get; set; }

    public int ActiveEvents { get; set; }

    public double? Temperature { get; set; }

    public double? RainProbability { get; set; }
}
