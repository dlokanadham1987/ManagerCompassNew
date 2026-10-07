# MC-07 Manager Compass — hackathon submission

RealPage Q3 AI Hackathon, use case "Manager Compass" (`02_Manager_Compass_Use Case Brief 1.pdf`). Deadline: 11:59 PM PT Thu 2026-09-17 — final package must land in the team's Teams channel, not stay local-only.

## Repo layout

- `00_Admin/` — `STATUS.md` (current state, blockers, test log), `DECISIONS.md` (decision/assumption log), `TEAM_CHARTER.md`, `HANDOFF.md`. Read `STATUS.md` and `DECISIONS.md` first — they're kept in lockstep with code changes.
- `03_Final_Submission/` — `README.md` (the submission-facing writeup) and `FINAL_SUBMISSION_CHECKLIST.md`.
- `app/backend/ManagerCompass.Api` — ASP.NET Core 8 Web API.
- `app/frontend/manager-compass-web` — Angular 18 (standalone components).

## Running the app locally

Both servers must be running or the UI loads with no data:

```
# 1. Backend — from app/backend/ManagerCompass.Api
dotnet run
# listens on http://localhost:5089

# 2. Frontend — from app/frontend/manager-compass-web (separate terminal)
ng serve
# listens on http://localhost:4200, proxies /api/* to localhost:5089 via proxy.conf.json
```

Start the backend **first**. If the Angular app is already running and shows empty views, check `Get-NetTCPConnection -State Listen | Where LocalPort -eq 5089` — a missing backend process is the most common cause of "data not loading."

If `dotnet build`/`dotnet run` fails with `MSB3027`/`MSB3021` (file locked), a previous `dotnet run` is still holding the exe — find and stop it (`Get-Process ManagerCompass.Api | Stop-Process -Force`) and rebuild.

## Data source and its privacy constraint

Real sponsor data lives at the path configured in `appsettings.json` → `SampleHrDataset:Path` (currently `OneDrive_1_9-16-2026\10_Sample HR Dataset`): `active_fte.xlsx`, `requisitions.xlsx`, `exits.xlsx`, `contractors_interns_fact_consultants.xlsx`. `SampleHrDatasetReader` reads these **live** via ClosedXML on API startup; `MockGuidanceDataService` falls back to a frozen copy of the same real numbers if that path isn't present on a machine.

**Hard constraint**, from each workbook's own "Field Guide" tab: only the small set of "direct identifier" columns (Employee Number, Employee Name, Preferred Name, Email Address, Supervisor Name, L2–L5, Functional Leader, Director, VP, SVP, EVP — plus the requisition-side equivalents) are genuinely linked per row; every other column (Gender, Job, Department, Region, etc.) was **independently shuffled** and cannot be safely combined with another column or with an identifier in the same row. Snapshot metrics and the Data Explorer's Identifiers grid both respect this — never build a feature that joins an attribute column to an identifier column.

Assistant/Playbooks guidance content is grounded in real HR documents from the same OneDrive folder, cited by real filename.

## Status

See `00_Admin/STATUS.md` for what's built, what's in progress, and current blockers (as of the last update: SharePoint upload of this whole folder into the team's Teams-connected SharePoint is the single biggest remaining blocker to a valid submission).
