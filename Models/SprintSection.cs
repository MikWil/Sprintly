namespace Sprintly.Models;

public class SprintSection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Label { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>Hex color, e.g. "#6366f1".</summary>
    public string Color { get; set; } = "#6366f1";
}
