namespace Surge.Models;

public class WeatherData
{
    public List<string> Times { get; set; } = [];

    public List<double> Temperatures { get; set; } = [];

    public List<double> RainProbabilities { get; set; } = [];
}
