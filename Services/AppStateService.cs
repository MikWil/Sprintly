using Sprintly.Models;

namespace Sprintly.Services;

public class AppStateService(LocalStorageService storage)
{
    private const string StorageKey = "sprintly-state";

    public AppState State { get; private set; } = new();
    public bool IsLoaded { get; private set; }

    public event Action? OnChange;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        if (IsLoaded) return;

        var saved = await storage.GetItemAsync<AppState>(StorageKey);
        if (saved != null)
            State = saved;
        else
            LoadSeedData();

        IsLoaded = true;
        Notify();
    }

    public async Task SaveAsync()
    {
        await storage.SetItemAsync(StorageKey, State);
    }

    // ── Team Members ───────────────────────────────────────────────────────────

    public async Task AddTeamMemberAsync(TeamMember member)
    {
        State.TeamMembers.Add(member);
        await SaveAsync();
        Notify();
    }

    public async Task UpdateTeamMemberAsync(TeamMember member)
    {
        var idx = State.TeamMembers.FindIndex(m => m.Id == member.Id);
        if (idx >= 0) State.TeamMembers[idx] = member;
        await SaveAsync();
        Notify();
    }

    public async Task RemoveTeamMemberAsync(string id)
    {
        State.TeamMembers.RemoveAll(m => m.Id == id);
        State.LeaveEntries.RemoveAll(l => l.TeamMemberId == id);
        await SaveAsync();
        Notify();
    }

    // ── Sprints ────────────────────────────────────────────────────────────────

    public async Task AddSprintAsync(Sprint sprint)
    {
        State.Sprints.Add(sprint);
        if (State.ActiveSprintId == null)
            State.ActiveSprintId = sprint.Id;
        await SaveAsync();
        Notify();
    }

    public async Task UpdateSprintAsync(Sprint sprint)
    {
        var idx = State.Sprints.FindIndex(s => s.Id == sprint.Id);
        if (idx >= 0) State.Sprints[idx] = sprint;
        await SaveAsync();
        Notify();
    }

    public async Task RemoveSprintAsync(string id)
    {
        State.Sprints.RemoveAll(s => s.Id == id);
        State.LeaveEntries.RemoveAll(l =>
        {
            var sprint = State.Sprints.FirstOrDefault(s => s.Id == id);
            return sprint != null &&
                   l.StartDate >= sprint.StartDate &&
                   l.EndDate <= sprint.EndDate;
        });
        if (State.ActiveSprintId == id)
            State.ActiveSprintId = State.Sprints.FirstOrDefault()?.Id;
        await SaveAsync();
        Notify();
    }

    public async Task SetActiveSprintAsync(string id)
    {
        State.ActiveSprintId = id;
        await SaveAsync();
        Notify();
    }

    public Sprint? GetActiveSprint() =>
        State.Sprints.FirstOrDefault(s => s.Id == State.ActiveSprintId);

    // ── Leave Entries ──────────────────────────────────────────────────────────

    public async Task AddLeaveAsync(LeaveEntry entry)
    {
        State.LeaveEntries.Add(entry);
        await SaveAsync();
        Notify();
    }

    public async Task UpdateLeaveAsync(LeaveEntry entry)
    {
        var idx = State.LeaveEntries.FindIndex(l => l.Id == entry.Id);
        if (idx >= 0) State.LeaveEntries[idx] = entry;
        await SaveAsync();
        Notify();
    }

    public async Task RemoveLeaveAsync(string id)
    {
        State.LeaveEntries.RemoveAll(l => l.Id == id);
        await SaveAsync();
        Notify();
    }

    public IEnumerable<LeaveEntry> GetLeaveForSprint(Sprint sprint) =>
        State.LeaveEntries.Where(l =>
            l.StartDate <= sprint.EndDate && l.EndDate >= sprint.StartDate);

    // ── Import / Export ────────────────────────────────────────────────────────

    public async Task<string> ExportJsonAsync() =>
        await storage.GetRawAsync(StorageKey) ?? "{}";

    public async Task ImportJsonAsync(string json)
    {
        await storage.SetRawAsync(StorageKey, json);
        var saved = await storage.GetItemAsync<AppState>(StorageKey);
        if (saved != null)
        {
            State = saved;
            Notify();
        }
    }

    // ── Settings ───────────────────────────────────────────────────────────────

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        State.Settings = settings;
        await SaveAsync();
        Notify();
    }

    // ── Private ────────────────────────────────────────────────────────────────

    private void Notify() => OnChange?.Invoke();

    private void LoadSeedData()
    {
        // Sample sprint: April 1–14 2026
        var sprint = new Sprint
        {
            Id = "seed-sprint-1",
            Name = "Sprint 42",
            StartDate = new DateOnly(2026, 4, 1),
            EndDate = new DateOnly(2026, 4, 14),
            ExcludeWeekends = true,
            HolidayDates = [new DateOnly(2026, 4, 6)] // Easter Monday
        };

        var alice = new TeamMember
        {
            Id = "seed-member-1",
            Name = "Alice Chen",
            Role = "Developer",
            HoursPerDay = 8,
            CapacityFactor = 0.8,
            IsActive = true
        };
        var bob = new TeamMember
        {
            Id = "seed-member-2",
            Name = "Bob Smith",
            Role = "Developer",
            HoursPerDay = 8,
            CapacityFactor = 1.0,
            IsActive = true
        };
        var carol = new TeamMember
        {
            Id = "seed-member-3",
            Name = "Carol Johnson",
            Role = "QA Engineer",
            HoursPerDay = 8,
            CapacityFactor = 0.9,
            IsActive = true
        };
        var david = new TeamMember
        {
            Id = "seed-member-4",
            Name = "David Lee",
            Role = "Tech Lead",
            HoursPerDay = 8,
            CapacityFactor = 0.7,
            IsActive = true
        };

        State.TeamMembers = [alice, bob, carol, david];
        State.Sprints = [sprint];
        State.ActiveSprintId = sprint.Id;

        // Alice takes Apr 1–2 off
        State.LeaveEntries =
        [
            new LeaveEntry
            {
                Id = "seed-leave-1",
                TeamMemberId = alice.Id,
                StartDate = new DateOnly(2026, 4, 1),
                EndDate = new DateOnly(2026, 4, 2),
                IsPartialDay = false,
                Type = "Leave",
                Note = "Family trip"
            },
            // Bob takes Apr 8 as a half day
            new LeaveEntry
            {
                Id = "seed-leave-2",
                TeamMemberId = bob.Id,
                StartDate = new DateOnly(2026, 4, 8),
                EndDate = new DateOnly(2026, 4, 8),
                IsPartialDay = true,
                Hours = 4,
                Type = "Leave",
                Note = "Doctor appointment"
            }
        ];
    }
}
