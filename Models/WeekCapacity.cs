namespace Sprintly.Models;

public class WeekCapacity
{
    public DateOnly Start { get; set; }
    public DateOnly End { get; set; }
    public int WorkingDays { get; set; }

    /// <summary>Raw member hours for this week, before buffer deduction.</summary>
    public double HoursGross { get; set; }

    /// <summary>Net hours after buffer deducted (pro-rated by day share).</summary>
    public double Hours { get; set; }

    /// <summary>Label of the sprint section this week falls in, if any.</summary>
    public string? SectionLabel { get; set; }

    /// <summary>Hex color of the sprint section this week falls in.</summary>
    public string? SectionColor { get; set; }
}
