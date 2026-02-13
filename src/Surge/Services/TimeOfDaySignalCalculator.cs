namespace Surge.Services;

public class TimeOfDaySignalCalculator : ITimeOfDaySignalCalculator
{
    public double Calculate(DateTime forecastHour)
    {
        var hour = forecastHour.Hour;

        return hour switch
        {
            >= 6 and < 10 => 0.0,   // Closed/quiet
            >= 10 and < 12 => 2.0,  // Morning trickle
            >= 12 and < 14 => 5.0,  // Lunch
            >= 14 and < 16 => 4.0,  // Afternoon
            >= 16 and < 18 => 6.0,  // After work
            >= 18 and < 21 => 8.0,  // Evening peak
            >= 21 and < 23 => 6.0,  // Wind-down
            _ => 0.0                // Late night / early morning
        };
    }
}
