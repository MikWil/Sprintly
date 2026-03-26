using Sprintly.Models;

namespace Sprintly.Services;

/// <summary>
/// Pure calculation engine — no side effects, easy to unit test.
/// Formula: capacity = (working_days - leave_days) * hours_per_day * capacity_factor
/// </summary>
public static class CapacityCalculator
{
    public static SprintCapacityResult Calculate(
        IEnumerable<TeamMember> members,
        Sprint sprint,
        IEnumerable<LeaveEntry> leaves)
    {
        var result = new SprintCapacityResult();
        var activeMembers = members.Where(m => m.IsActive).ToList();
        var allLeaves = leaves.ToList();

        result.WorkingDaysInSprint = CountWorkingDays(sprint);

        foreach (var member in activeMembers)
        {
            var memberLeaves = allLeaves.Where(l => l.TeamMemberId == member.Id);
            var leaveDays = CountLeaveDays(member, sprint, memberLeaves);
            var effectiveDays = Math.Max(0, result.WorkingDaysInSprint - leaveDays);
            var hours = Math.Round(effectiveDays * member.HoursPerDay * member.CapacityFactor, 1);

            result.HoursByPerson[member.Name] = hours;
            result.TotalHours += hours;

            if (!result.HoursByRole.TryAdd(member.Role, hours))
                result.HoursByRole[member.Role] += hours;
        }

        result.TotalHoursGross = Math.Round(result.TotalHours, 1);

        // Apply team-level buffers
        var totalBufferPct = sprint.Buffers.Sum(b => b.Percentage);
        if (totalBufferPct > 0)
        {
            result.BufferPercentage = totalBufferPct;
            result.BufferHours = Math.Round(result.TotalHoursGross * totalBufferPct / 100.0, 1);
            foreach (var buf in sprint.Buffers)
                result.BufferBreakdown[buf.Label] = Math.Round(result.TotalHoursGross * buf.Percentage / 100.0, 1);
            result.TotalHours = Math.Round(result.TotalHoursGross * Math.Max(0, (100.0 - totalBufferPct) / 100.0), 1);
        }
        else
        {
            result.TotalHours = result.TotalHoursGross;
        }

        result.TotalPersonDays = result.WorkingDaysInSprint > 0
            ? Math.Round(result.TotalHours / 8.0, 1)
            : 0;

        result.CapacityByWeek = BuildWeeklyBreakdown(activeMembers, sprint, allLeaves, result.BufferPercentage);

        AddWarnings(result, activeMembers, sprint);

        return result;
    }

    public static int CountWorkingDays(Sprint sprint)
    {
        var days = 0;
        var current = sprint.StartDate;
        while (current <= sprint.EndDate)
        {
            if (IsWorkingDay(current, sprint))
                days++;
            current = current.AddDays(1);
        }
        return days;
    }

    /// <summary>Returns leave as fractional days (1.0 = full day, 0.5 = half day, etc.)</summary>
    public static double CountLeaveDays(TeamMember member, Sprint sprint, IEnumerable<LeaveEntry> memberLeaves)
    {
        double leaveDays = 0;

        foreach (var leave in memberLeaves)
        {
            var current = leave.StartDate;
            while (current <= leave.EndDate)
            {
                if (current >= sprint.StartDate && current <= sprint.EndDate && IsWorkingDay(current, sprint))
                {
                    leaveDays += leave.IsPartialDay
                        ? leave.Hours / member.HoursPerDay
                        : 1.0;
                }
                current = current.AddDays(1);
            }
        }

        return leaveDays;
    }

    public static bool IsWorkingDay(DateOnly date, Sprint sprint)
    {
        if (sprint.ExcludeWeekends && IsWeekend(date))
            return false;
        if (sprint.HolidayDates.Contains(date))
            return false;
        return true;
    }

    public static bool IsWeekend(DateOnly date) =>
        date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    /// <summary>
    /// Splits the sprint into calendar weeks (Mon–Sun) and computes capacity per week.
    /// The buffer is applied pro-rata to each week's gross hours.
    /// Section label/color are resolved from the section that contains the week's start date.
    /// </summary>
    private static List<WeekCapacity> BuildWeeklyBreakdown(
        List<TeamMember> activeMembers,
        Sprint sprint,
        List<LeaveEntry> allLeaves,
        double bufferPct)
    {
        var weeks = new List<WeekCapacity>();

        // Walk ISO calendar weeks that intersect the sprint
        var weekStart = sprint.StartDate;
        while (weekStart.DayOfWeek != DayOfWeek.Monday)
            weekStart = weekStart.AddDays(-1);

        while (weekStart <= sprint.EndDate)
        {
            var weekEnd = weekStart.AddDays(6);

            // Clamp to sprint dates
            var sliceStart = weekStart < sprint.StartDate ? sprint.StartDate : weekStart;
            var sliceEnd   = weekEnd   > sprint.EndDate   ? sprint.EndDate   : weekEnd;

            var sliceSprint = new Sprint
            {
                StartDate       = sliceStart,
                EndDate         = sliceEnd,
                ExcludeWeekends = sprint.ExcludeWeekends,
                HolidayDates    = sprint.HolidayDates.Where(h => h >= sliceStart && h <= sliceEnd).ToList()
            };

            var workingDays = CountWorkingDays(sliceSprint);

            double gross = 0;
            foreach (var member in activeMembers)
            {
                var memberLeaves = allLeaves.Where(l => l.TeamMemberId == member.Id);
                var leaveDays    = CountLeaveDays(member, sliceSprint, memberLeaves);
                var effective    = Math.Max(0, workingDays - leaveDays);
                gross += effective * member.HoursPerDay * member.CapacityFactor;
            }

            gross = Math.Round(gross, 1);
            var net = bufferPct > 0
                ? Math.Round(gross * Math.Max(0, (100.0 - bufferPct) / 100.0), 1)
                : gross;

            // Resolve section by the first working day (or slice start)
            var sectionDay  = sliceStart;
            var section     = sprint.Sections.FirstOrDefault(s => s.StartDate <= sectionDay && s.EndDate >= sectionDay)
                           ?? sprint.Sections.FirstOrDefault(s => s.StartDate <= sliceEnd   && s.EndDate >= sliceStart);

            weeks.Add(new WeekCapacity
            {
                Start        = sliceStart,
                End          = sliceEnd,
                WorkingDays  = workingDays,
                HoursGross   = gross,
                Hours        = net,
                SectionLabel = section?.Label,
                SectionColor = section?.Color
            });

            weekStart = weekStart.AddDays(7);
        }

        return weeks;
    }

    private static void AddWarnings(SprintCapacityResult result, List<TeamMember> activeMembers, Sprint sprint)
    {
        if (activeMembers.Count == 0)
        {
            result.Warnings.Add("No active team members found.");
            return;
        }

        if (result.WorkingDaysInSprint == 0)
            result.Warnings.Add("Sprint has no working days — check dates and holidays.");

        if (result.BufferPercentage >= 100)
            result.Warnings.Add($"Total buffer is {result.BufferPercentage:F0}% — no net capacity remains for sprint work.");
        else if (result.BufferPercentage > 60)
            result.Warnings.Add($"Total buffer is {result.BufferPercentage:F0}% which is very high. Verify buffer settings.");

        if (result.TotalHours == 0 && result.BufferPercentage < 100)
            result.Warnings.Add("Total capacity is zero. Review member settings and leave entries.");
        else if (result.TotalHours < 8 && result.TotalHoursGross >= 8)
            result.Warnings.Add("Net capacity is very low after buffers. Verify buffer percentages.");
        else if (result.TotalHours < 8)
            result.Warnings.Add("Total capacity is very low (< 8 h). Verify team availability.");

        foreach (var member in activeMembers)
        {
            if (result.HoursByPerson.TryGetValue(member.Name, out var h) && h == 0)
                result.Warnings.Add($"{member.Name} has zero capacity for this sprint.");
        }
    }
}
