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
        var sprint = new Sprint
        {
            Id          = "6af5a82d-3c97-417d-81f4-e6080220c85f",
            Name        = "Sprint 20",
            StartDate   = new DateOnly(2026, 3, 30),
            EndDate     = new DateOnly(2026, 4, 19),
            ExcludeWeekends = true,
            HolidayDates =
            [
                new DateOnly(2026, 4, 2), // Good Friday
                new DateOnly(2026, 4, 3), // Holy Saturday
                new DateOnly(2026, 4, 6)  // Easter Monday
            ],
            Buffers =
            [
                new SprintBuffer
                {
                    Id         = "62ae786c-ac04-45a4-97fd-59f3f9a2ca5d",
                    Label      = "Bug fixing and meetings",
                    Percentage = 20
                }
            ]
        };

        State.TeamMembers =
        [
            new TeamMember { Id = "seed-member-1",                        Name = "Justinas", Role = "Developer", HoursPerDay = 8, CapacityFactor = 1.0, IsActive = true },
            new TeamMember { Id = "seed-member-2",                        Name = "Tim",      Role = "Tech Lead", HoursPerDay = 8, CapacityFactor = 1.0, IsActive = true },
            new TeamMember { Id = "seed-member-3",                        Name = "Emilie",   Role = "Developer", HoursPerDay = 8, CapacityFactor = 1.0, IsActive = true },
            new TeamMember { Id = "seed-member-4",                        Name = "Alex",     Role = "Developer", HoursPerDay = 8, CapacityFactor = 1.0, IsActive = true },
            new TeamMember { Id = "720fa97a-ec85-4eef-9671-5f04eed07de4", Name = "LeeAnn",  Role = "Developer", HoursPerDay = 8, CapacityFactor = 1.0, IsActive = true }
        ];

        State.Sprints        = [sprint];
        State.ActiveSprintId = sprint.Id;

        State.LeaveEntries =
        [
            // Justinas: Mar 30 – Apr 5
            new LeaveEntry { Id = "860422c0-0d36-4a83-92bb-6169085ec1e2", TeamMemberId = "seed-member-1",                        StartDate = new DateOnly(2026, 3, 30), EndDate = new DateOnly(2026, 4, 5),  IsPartialDay = false, Type = "Leave" },
            // Alex: Apr 13 – Apr 19
            new LeaveEntry { Id = "fb129289-e45c-4f4e-8902-b63f33ba3f99", TeamMemberId = "seed-member-4",                        StartDate = new DateOnly(2026, 4, 13), EndDate = new DateOnly(2026, 4, 19), IsPartialDay = false, Type = "Leave" },
            // Emilie: Apr 6 – Apr 7
            new LeaveEntry { Id = "bcb53cbb-d02d-446f-9e90-721797d83efe", TeamMemberId = "seed-member-3",                        StartDate = new DateOnly(2026, 4, 6),  EndDate = new DateOnly(2026, 4, 7),  IsPartialDay = false, Type = "Leave" },
            // LeeAnn: Apr 6 – Apr 7
            new LeaveEntry { Id = "26cbe5ea-5a14-4e00-8247-c5d05d1f9c76", TeamMemberId = "720fa97a-ec85-4eef-9671-5f04eed07de4", StartDate = new DateOnly(2026, 4, 6),  EndDate = new DateOnly(2026, 4, 7),  IsPartialDay = false, Type = "Leave" }
        ];
    }
}
