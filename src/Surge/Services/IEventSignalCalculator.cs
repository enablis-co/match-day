using Surge.Clients.Dtos;

namespace Surge.Services;

public interface IEventSignalCalculator
{
    double Calculate(DateTime forecastHour, List<EventDto> events);
}
