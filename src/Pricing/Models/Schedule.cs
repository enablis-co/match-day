namespace Pricing.Models;

public class Schedule
{
    public List<DayOfWeek> Days { get; set; } = [];
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
