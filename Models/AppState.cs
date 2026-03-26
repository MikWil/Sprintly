namespace Sprintly.Models;

public class AppState
{
    public List<TeamMember> TeamMembers { get; set; } = [];
    public List<Sprint> Sprints { get; set; } = [];
    public List<LeaveEntry> LeaveEntries { get; set; } = [];
    public AppSettings Settings { get; set; } = new();
    public string? ActiveSprintId { get; set; }
}
