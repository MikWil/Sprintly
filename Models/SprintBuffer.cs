namespace Sprintly.Models;

public class SprintBuffer
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Label { get; set; } = "";
    public double Percentage { get; set; } = 10;
}
