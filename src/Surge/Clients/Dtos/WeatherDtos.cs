namespace Surge.Clients.Dtos;

public class OpenMeteoResponse
{
    public HourlyData? Hourly { get; set; }
}

public class HourlyData
{
    public List<string> Time { get; set; } = [];

    public List<double> Temperature_2m { get; set; } = [];

    public List<double> Precipitation_probability { get; set; } = [];
}
