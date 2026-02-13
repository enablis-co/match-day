namespace Surge.Services;

public interface ITimeOfDaySignalCalculator
{
    double Calculate(DateTime forecastHour);
}
