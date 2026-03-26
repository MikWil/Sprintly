# Sprint Capacity Planner

A lightweight, static Blazor WebAssembly app that calculates realistic sprint capacity based on team availability, personal leave, and public holidays.

> All data is stored in your browser's **LocalStorage** — no backend, no accounts, no cloud sync.

---

## Features

| Feature | Description |
|---------|-------------|
| **Team management** | Add/edit/remove members with role, hours/day, and capacity factor |
| **Sprint planning** | Create sprints with date ranges; click calendar days to mark red days |
| **Leave management** | Track full or partial-day leave per person per sprint |
| **Capacity results** | Total hours, person-days, breakdown by role and person, warnings |
| **Export / Import** | Download and restore full app state as JSON |

**Capacity formula:**
```
Capacity per person =
  (working_days_in_sprint - leave_days) × hours_per_day × capacity_factor
```
- Weekends are excluded by default
- Public holidays / red days are excluded for all team members
- Leave on weekends or holidays is not counted
- Inactive members are excluded

---

## Tech stack

- **Blazor WebAssembly** (.NET 10)
- **Bootstrap 5** (local) + **Bootstrap Icons** (CDN)
- **LocalStorage** via JS interop (no Blazor library dependency)
- **GitHub Actions** + **GitHub Pages** for hosting
- **xUnit** for unit tests

---

## Local development

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Run

```bash
cd Sprintly
dotnet run
```

App opens at `http://localhost:5000` (or the port shown in terminal).

### Run tests

```bash
cd ../Sprintly.Tests
dotnet test
```

Or from the solution root:

```bash
dotnet test Sprintly.slnx
```

---

## GitHub Pages deployment

### One-time setup

1. Push the repo to GitHub (repo name: `Sprintly`).
2. Go to **Settings → Pages → Source** and select **GitHub Actions**.
3. That's it — the workflow deploys on every push to `main`.

### Live URL

```
https://<your-github-username>.github.io/Sprintly/
```

### How it works

The `.github/workflows/deploy.yml` workflow:
1. Builds the Blazor WASM app with `dotnet publish`
2. Patches `<base href="/" />` → `<base href="/Sprintly/" />` in the output
3. Copies `index.html` → `404.html` for client-side routing fallback
4. Deploys via `actions/deploy-pages`

> **Important:** Do not commit `<base href="/Sprintly/" />` — the workflow patches this at build time. Keep `<base href="/" />` for local development.

---

## Project structure

```
Sprintly/
├── .cursor/skills/          # Project-specific Claude skills
│   ├── requirements-guard/  # MVP scope guardrails
│   ├── frontend-ui/         # Blazor/Bootstrap UI patterns
│   ├── test-checklist/      # What to test and how
│   └── gh-pages-deploy/     # Deployment guide
├── .github/workflows/
│   └── deploy.yml           # GitHub Pages CI/CD
├── Components/
│   ├── CalendarView.razor   # Sprint calendar with holiday toggling
│   └── CapacityCard.razor   # KPI metric card
├── Layout/
│   ├── MainLayout.razor     # App shell with export/import
│   └── NavMenu.razor        # Sidebar navigation
├── Models/
│   ├── AppState.cs          # Root state object
│   ├── AppSettings.cs
│   ├── LeaveEntry.cs
│   ├── Sprint.cs
│   ├── SprintCapacityResult.cs
│   └── TeamMember.cs
├── Pages/
│   ├── Home.razor           # Dashboard / results
│   ├── TeamPage.razor       # Team management
│   ├── SprintPage.razor     # Sprint management + calendar
│   └── LeavePage.razor      # Leave management
├── Services/
│   ├── AppStateService.cs   # State management + LocalStorage + seed data
│   ├── CapacityCalculator.cs  # Pure calculation engine (unit-tested)
│   └── LocalStorageService.cs
└── wwwroot/
    ├── 404.html             # SPA routing fallback for GitHub Pages
    ├── css/app.css
    └── js/interop.js        # JS download helper

Sprintly.Tests/
└── CapacityCalculatorTests.cs  # xUnit tests for calculation logic
```

---

## Sample data

On first load (empty LocalStorage) the app seeds:

| Member | Role | h/day | Capacity |
|--------|------|-------|----------|
| Alice Chen | Developer | 8 | 80% |
| Bob Smith | Developer | 8 | 100% |
| Carol Johnson | QA Engineer | 8 | 90% |
| David Lee | Tech Lead | 8 | 70% |

**Sprint 42** — Apr 1–14 2026 · Easter Monday (Apr 6) as red day

| Leave | Duration |
|-------|----------|
| Alice Apr 1–2 | 2 full days |
| Bob Apr 8 | 4 h partial |

Expected total capacity: **228.0 h / 28.5 person-days**

---

## Data persistence

All data is saved to `localStorage["sprintly-state"]` as JSON on every change.

### Export
Click **Export** in the top bar → downloads `sprintly-backup-YYYY-MM-DD.json`.

### Import
Click **Import** in the top bar → select a previously exported JSON file → restores full state.

---

## Claude skills

This repo includes project-specific Claude skills in `.cursor/skills/`:

| Skill | Purpose |
|-------|---------|
| `requirements-guard` | Keeps changes within MVP scope |
| `frontend-ui` | Blazor/Bootstrap patterns and conventions |
| `test-checklist` | What and how to test |
| `gh-pages-deploy` | Deployment setup and troubleshooting |

---

## Non-goals (MVP)

- No authentication
- No backend or database
- No cloud sync
- No Jira / calendar integrations
- No AI features
- No multi-user support
