# Manager Compass

**Team MC-07 · RealPage Q3 AI Hackathon**

A manager-facing HR guidance assistant. Managers ask real people/process questions and get answers grounded in actual RealPage HR documents, a live-computed People Snapshot, and a signal-driven Playbooks/Adoption layer — not invented content, and never a pay, performance, legal, or employment decision made by the app itself.

## Views

| View | What it does |
| --- | --- |
| Assistant | Ask a question or pick a topic; answers cite a real source document, with a checklist and a named escalation contact. Guardrails refuse individual pay/performance/health questions and route to HR. |
| People Snapshot | Headcount, requisitions, exits, contingent workforce, and region breakdown — computed live from the real sample HR dataset at startup. |
| Playbooks | The full guidance library, grouped by topic, with per-topic checklist progress. |
| 1:1s | Schedule 1:1s with direct reports (downloads a real `.ics` file for Outlook — no OAuth/Graph API), prep with pre-notes pulled forward from each report's own open action items, and log append-only post-notes after the conversation. |
| Escalation Directory | Every guardrail in one table: exactly when to hand off to HR, and to whom. |
| Leadership Update | A baseball-card-style operating review with a computed "Team Pulse" badge. |
| Adoption & Impact | Usage KPIs with a per-topic drill-down on each card, plus a full 9-topic breakdown table. |
| Data Explorer | A live, filterable grid over the dataset's genuinely-linked identifier columns. |

## Stack

- **Backend:** ASP.NET Core 8 Web API (`app/backend/ManagerCompass.Api`)
- **Frontend:** Angular 18, standalone components (`app/frontend/manager-compass-web`)

## Running locally

```
# 1. Backend — from app/backend/ManagerCompass.Api
dotnet run
# listens on http://localhost:5089

# 2. Frontend — from app/frontend/manager-compass-web (separate terminal)
npm install   # first time only
ng serve
# listens on http://localhost:4200, proxies /api/* to localhost:5089
```

Start the backend first — if the Angular app loads with no data, the backend usually isn't running yet.

## Data

The real sample HR dataset ships inside the repo at `app/backend/Resources/Sample HR Dataset/` and is read **live** by `SampleHrDatasetReader` on API startup (`appsettings.json` → `SampleHrDataset:Path`, a relative path — no per-machine configuration needed). If that folder is ever unreachable, the API falls back to a frozen copy of the same real numbers so the app still runs.

**Privacy constraint** (from the dataset's own "Field Guide" tab): only a small set of "direct identifier" columns (Employee Number, Name, Email, Supervisor, leadership chain) are genuinely linked per row — every other column was independently shuffled. Only single-column tallies or that verified identifier set may ever be shown together; never join a shuffled attribute column to an identifier.

## 1:1s

Personal, manager-owned scheduling and notes — never an official performance record, and separate from the sponsor dataset:

- **Storage:** everything (roster, meetings, notes) lives in the browser's `localStorage` only. No backend, no database, nothing sent to the API. Clearing site data resets it.
- **Calendar integration:** "Schedule Meeting" generates and downloads a standard `.ics` file rather than calling Microsoft Graph. That's deliberate — creating a real Outlook event would need an Azure AD app registration, admin-consented `Calendars.ReadWrite` permission, and a stored client secret, none of which are appropriate to set up for a hackathon prototype. The manager opens the downloaded file and confirms the event in their own Outlook.
- **"AI summary" is a mock.** There's no real per-employee activity feed to summarize from (the sample HR dataset has no such data), so clicking it shows a simulated loading state and a clearly-labeled illustrative placeholder — not a real model call.
- **Post notes are append-only.** Every save adds a new dated entry to that meeting's own log rather than overwriting the previous one, so reopening a meeting later shows the full running history.
- Seeded on first visit with three starter team members and one sample upcoming meeting each, so the tab isn't empty before you've scheduled anything of your own.

## `references/`

Sponsor reference material (use case brief, HR source documents, participant package) — kept on disk for local use but excluded from version control via `.gitignore`. It isn't needed to run the app.
