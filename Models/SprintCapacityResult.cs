namespace Sprintly.Models;

public class SprintCapacityResult
{
    /// <summary>Sum of all member hours before any buffer deduction.</summary>
    public double TotalHoursGross { get; set; }

    /// <summary>Net hours available for sprint work after buffer deduction.</summary>
    public double TotalHours { get; set; }

    public double TotalPersonDays { get; set; }
    public int WorkingDaysInSprint { get; set; }

    /// <summary>Total buffer percentage applied (sum of all SprintBuffer.Percentage).</summary>
    public double BufferPercentage { get; set; }

    /// <summary>Hours deducted by all buffers combined.</summary>
    public double BufferHours { get; set; }

    /// <summary>Per-buffer breakdown: label → hours reserved.</summary>
    public Dictionary<string, double> BufferBreakdown { get; set; } = [];

    public Dictionary<string, double> HoursByRole { get; set; } = [];
    public Dictionary<string, double> HoursByPerson { get; set; } = [];
    public List<WeekCapacity> CapacityByWeek { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
