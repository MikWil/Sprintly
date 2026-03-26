namespace Sprintly.Models;

public class TeamMember
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public double HoursPerDay { get; set; } = 8;
    public double CapacityFactor { get; set; } = 1.0;
    public bool IsActive { get; set; } = true;
}
