namespace Sprintly.Models;

public class Sprint
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(13));
    public bool ExcludeWeekends { get; set; } = true;
    public List<DateOnly> HolidayDates { get; set; } = [];
    public List<SprintBuffer> Buffers { get; set; } = [];
    public List<SprintSection> Sections { get; set; } = [];
}
