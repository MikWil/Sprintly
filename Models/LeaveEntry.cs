namespace Sprintly.Models;

public class LeaveEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TeamMemberId { get; set; } = "";
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public bool IsPartialDay { get; set; }
    public double Hours { get; set; }
    public string Type { get; set; } = "Leave";
    public string Note { get; set; } = "";
}
