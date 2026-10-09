# StudyHub

StudyHub is a Blazor Web App built to help computer science students organize their studies and
improve learning efficiency. It centralizes the day-to-day work of studying — courses, planning,
documents, notes, and (eventually) AI-assisted learning tools — into a single application.

The project is being built with two goals in mind:

1. **A daily productivity tool** — a place to manage semesters, courses, study plans, documents,
   and notes without juggling several disconnected apps.
2. **A portfolio project** — a demonstration of a clean, layered .NET architecture (a Composite
   Component Architecture Pattern) built with modern .NET, Blazor, and Entity Framework Core.

## Vision

StudyHub aims to grow into a complete personal learning companion: a single application where a
student can plan their semester, track courses and study sessions, manage documents and
Markdown notes, and use AI assistance to reinforce (not replace) their own understanding of the
material — including generated summaries, review questions, and exportable flashcards.

The long-term goal is a learning platform students actually want to open every day, backed by an
architecture clean enough to keep evolving without accumulating unmanageable complexity.

## Status

StudyHub is in early, active development. The solution currently contains the foundational
project layout (Shared, Data, Logic, Infrastructure, UI, Tests) wired together per the target
architecture, running on the default Blazor Web App template. The Data layer is wired up to a
SQLite database via EF Core (Code First, no feature tables yet), and the app is prepared for
Docker-based deployment. Feature work (authentication, course/study management, UI, AI features)
has not started yet — see [Features](#features) and [Roadmap](#roadmap) below for what's
implemented versus planned.

---

## Features

### Current Features

- Solution scaffold following the target layered architecture (Shared, Data, Logic, Infrastructure,
  UI, Api, Tests projects, wired via project references).
- A separate `StudyHub.Api` ASP.NET Core Web API project between the UI and the Data layer; the
  Blazor UI calls it over HTTP instead of referencing Data/Infrastructure directly.
- Blazor Server UI project (Interactive Server render mode) running on .NET 10.
- SQLite persistence via EF Core (Code First), with the database file location configurable and
  migrations applied automatically on Api startup.
- Docker/Docker Compose deployment with separate UI and Api containers; the SQLite database is
  persisted outside the Api container via a bind mount.
- AI flashcard generation from a Markdown note (Claude via the Anthropic API), with review/editing
  in the app before saving the cards to a deck (see
  [Configure AI features](#configure-ai-features-flashcards)).
- Flashcard decks studied with spaced repetition (Anki's SM-2 scheduler with Anki's default
  options, daily limits, Again/Hard/Good/Easy with keyboard shortcuts), import of Anki's text
  export ("Notes in Plain Text", CSV/TXT/TSV), deck export in the same format, and a "Flashcards
  due today" card on the Dashboard (see [Flashcard study day](#flashcard-study-day)).
- A calendar of planned study sessions with a month view and an ISO week view (Monday first):
  sessions are added, edited and deleted in the calendar, can belong to a course or a semester,
  are shown in the course color, overlapping sessions sit side by side in the week view, and the
  Dashboard shows today's sessions (see [Calendar time zone](#calendar-time-zone)).
- Exams and deadlines in the calendar: an exam is all-day or has a start time and a duration, a
  deadline has an optional due time. They stand out in the month and week views and the day
  panel, and the Dashboard lists the next ones with a countdown.

No user-facing product features (authentication, course management, study planning, etc.) have
been implemented yet.

### Planned Features

- ASP.NET Core Identity–based authentication and user accounts.
- Semester and course management.
- Study planning and study session tracking.
- Document management and a document library.
- Markdown-based notes.
- Recurring study sessions in the calendar.

### Future Ideas

- AI-assisted learning: summaries, explanations, and review questions generated from study
  material.
- Flashcard generation with Anki export.
- Learning analytics dashboard.
- Chat-based, document-grounded learning assistant (RAG).
- Handwritten note import and OCR.
- Local LLM support and cloud synchronization.

---

## Technology Stack

- **.NET 10**
- **Blazor Web App** (Interactive Server render mode)
- **Entity Framework Core** — Code First
- **SQLite** — persistence layer; the database is a single portable file
- **Docker / Docker Compose** — containerized deployment
- **ASP.NET Core Identity** *(planned)*
- **MudBlazor** *(planned)* — component library for the UI layer
- **xUnit** *(planned)* — unit testing
- **FluentValidation** *(planned)*
- **Mapster** *(planned)*

Items marked *(planned)* are part of the target stack described in [`CLAUDE.md`](./CLAUDE.md) but
are not yet present in the codebase.

> **Note:** This document previously listed SQL Server as the planned database. SQLite was adopted
> instead for portability — the entire database is a single file that can be backed up, moved, or
> shipped alongside a container without a separate database server.

---

## Architecture

StudyHub follows a **Composite Component Architecture Pattern** inspired by Clean Architecture.
Each feature is composed across the layers below and assembled through contracts (interfaces),
rather than direct references between implementations.

### Layers

**Shared**
Cross-cutting types shared across layers (e.g. common constants, base types). Kept minimal by
design — a feature-local type only moves here once it's genuinely needed by multiple layers.

**Data**
EF Core models, entity configurations, and persistence. Data-access concerns only — no business
logic.

**Logic**
The application core, split into three sub-layers:

- **Domain** — core business models and rules. Pure business logic with no external dependencies.
- **Integration** — adapters and clients for external systems and APIs.
- **Business** — application use-cases; orchestrates Domain and Integration logic and coordinates
  workflows across features. Must not contain infrastructure details.

**Infrastructure**
Implementations of external services (AI providers, email, storage, third-party APIs) that
fulfill contracts defined in the Logic layer.

**Api**
A separate ASP.NET Core Web API host exposing REST endpoints over the Business layer's contracts.
Owns the composition root (DI wiring for Data/Infrastructure/Business) and applies EF Core
migrations at startup. Deployed as its own container, independent of the UI host.

**UI**
Presentation only, built with Blazor Server. Razor components stay thin and call into the Business
layer's contracts; no business logic lives here. UI no longer talks to Data/Infrastructure
directly — it calls the Api host over HTTP through an adapter that satisfies the same contracts.

**Tests**
Automated tests, primarily unit tests for business logic (xUnit).

### Contracts-First Principle

All communication between layers happens through contracts (interfaces):

- Business and UI layers never instantiate implementations directly — they depend only on
  abstractions.
- Concrete implementations live in the Infrastructure or Integration layer.

This keeps the architecture testable, loosely coupled, and lets services (AI providers, storage,
external APIs) be swapped without touching consumers.

For the full set of coding standards and architectural rules, see [`CLAUDE.md`](./CLAUDE.md).

---

## Project Structure

```
StudyHub/
├── src/
│   ├── Shared/
│   │   └── StudyHub.Shared/            # Cross-cutting shared types
│   ├── Data/
│   │   └── StudyHub.Data/              # ApplicationDbContext, EF Core migrations & persistence
│   ├── Logic/
│   │   ├── StudyHub.Logic.Domain/      # Core business models & rules
│   │   ├── StudyHub.Logic.Integration/ # External system adapters/clients
│   │   └── StudyHub.Logic.Business/    # Use-cases & orchestration
│   ├── Infrastructure/
│   │   └── StudyHub.Infrastructure/    # Implementations of external services
│   └── UI/
│       ├── StudyHub.UI/                # Blazor Server app (presentation only)
│       └── StudyHub.Api/               # ASP.NET Core Web API host (composition root + endpoints)
├── tests/
│   └── StudyHub.Tests/                 # Automated tests (xUnit)
├── docs/                               # Architecture and reference documentation
├── data/                               # SQLite database (bind-mounted, gitignored, Docker only)
├── Dockerfile                          # Multi-stage build for both the UI and Api hosts
├── docker-compose.yml                  # UI + Api containers and the database's persistent volume
├── StudyHub.slnx                       # Solution file
└── CLAUDE.md                           # Architecture & coding guidelines
```

---

## Getting Started

> These steps reflect the current, minimal state of the project. Identity/authentication and
> feature-specific persistence will be filled in as those pieces are implemented.

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) — for running locally without Docker.
- [Docker](https://www.docker.com/) and Docker Compose — for containerized deployment.
- No separate database server is required. SQLite is embedded — the database is a single file
  created automatically on first run.

### Clone the repository

```bash
git clone <repository-url>
cd StudyHub
```

### Restore dependencies

```bash
dotnet restore
```

### Configure the database

The database is owned by `StudyHub.Api` (not the UI). Its location is set via the
`ConnectionStrings:DefaultConnection` configuration value:

- **Production / Docker**: read from `src/UI/StudyHub.Api/appsettings.json` —
  `Data Source=/app/data/StudyHub.db`, matching the container path bind-mounted from `./data` on
  the host (see [Running with Docker Compose](#running-with-docker-compose)).
- **Development** (`dotnet run` locally): **not** stored in `appsettings.Development.json` — it's
  kept in the [.NET Secret Manager](https://learn.microsoft.com/aspnet/core/security/app-secrets)
  (user secrets), so it never ends up in source control even by accident. Set it once per machine:

  ```bash
  dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Data Source=App_Data/StudyHub.db" \
    --project src/UI/StudyHub.Api/StudyHub.Api.csproj
  ```

  The path is resolved relative to the `StudyHub.Api` project directory, and the file/parent folder
  are created automatically on first run if they don't exist. User secrets only apply when
  `ASPNETCORE_ENVIRONMENT=Development` (the default for `dotnet run`) — Docker/Production always
  uses the value from `appsettings.json`.

Pending migrations are applied automatically at Api startup — there is no separate manual step for
a fresh environment.

### Configure AI features (flashcards)

Flashcard generation calls Claude through the Anthropic API from `StudyHub.Api` (the UI never talks
to Anthropic). All settings live in the `Anthropic` section of `src/UI/StudyHub.Api/appsettings.json`.
`Anthropic:ApiKey` is committed **empty** — set the real key locally as a user secret, which
overrides the empty value when `ASPNETCORE_ENVIRONMENT=Development`:

```bash
dotnet user-secrets set "Anthropic:ApiKey" "<your key>" \
  --project src/UI/StudyHub.Api/StudyHub.Api.csproj
```

Without a key the app still starts; the generator page (`/flashcards/generate`) then shows an
"AI not configured" message. Decks, study sessions and the Anki import work without a key.

The model is chosen per generation on the generator page. The choices come from
`Anthropic:Models` (a list of `Id` + `DisplayName`), preselected with `Anthropic:DefaultModel`
(`claude-sonnet-5-5`); the Api rejects any model that isn't in that list. Further settings:
`Anthropic:Effort` (`medium`) and `Anthropic:MaxTokens`. Each generation is a paid API call (Opus
costs about twice as much as Sonnet); note content is sent to Anthropic.

### Flashcard study day

Like Anki's "next day starts at 4", a study day for flashcards starts at a fixed hour in a fixed
time zone. Both are set in the `Flashcards` section of `src/UI/StudyHub.Api/appsettings.json`
(`TimeZone`: `Europe/Berlin`, `DayStartHour`: `4`) and are validated at Api startup — an unknown
IANA time zone id or an hour outside 0–23 stops the Api with an error.

### Calendar time zone

Study sessions are stored as local wall-clock times (date, start, duration), so a session planned
for 09:00 stays at 09:00 across daylight-saving changes. The time zone only decides which day is
"today"; it is set in the `Calendar` section of `src/UI/StudyHub.Api/appsettings.json`
(`TimeZone`: `Europe/Berlin`) and validated at Api startup like the flashcard time zone.

### Run the application (locally, without Docker)

The UI calls the Api over HTTP, so both processes need to run at the same time (in separate
terminals):

```bash
dotnet run --project src/UI/StudyHub.Api/StudyHub.Api.csproj
dotnet run --project src/UI/StudyHub.UI/StudyHub.UI.csproj
```

On first run this creates `src/UI/StudyHub.Api/App_Data/StudyHub.db` and applies all migrations.

The UI needs `Api:BaseAddress` to point at the Api's local dev URL instead of the Docker-only
`studyhub-api` hostname from `appsettings.json`. This override is gitignored (it's per-machine), so
create it once from the checked-in template:

```bash
cp src/UI/StudyHub.UI/appsettings.Development.json.example src/UI/StudyHub.UI/appsettings.Development.json
```

This points `Api:BaseAddress` at `http://localhost:5250/` (see
`src/UI/StudyHub.Api/Properties/launchSettings.json`). Without it — e.g. when launching both
projects directly from an IDE like Rider instead of via `dotnet run` from a shell with this file
already present — the UI throws `SocketException: ... studyhub-api ...` because that hostname only
resolves inside the Docker Compose network.

### Run the tests

```bash
dotnet test
```

---

## Running with Docker Compose

This is the recommended way to run StudyHub as a portable, self-contained deployment.

```bash
docker compose up --build -d
```

To enable flashcard generation, export `ANTHROPIC_API_KEY` in the shell (or put it in a `.env`
file next to `docker-compose.yml`, which is not committed) before starting; `docker-compose.yml`
passes it to the `studyhub-api` container only, as `Anthropic__ApiKey` (user secrets are not used
outside Development).

This builds two images — `studyhub-api` and `studyhub` — and starts both containers. The UI is
published on <http://localhost:8080>; the Api container is only reachable from the UI container
over the internal Docker network (`http://studyhub-api:8080/`), not published to the host. The UI
container waits for the Api container to report healthy (via its `/health` endpoint) before
starting, so migrations have finished by the time the UI accepts traffic.

The SQLite database is stored at `/app/data/StudyHub.db` inside the `studyhub-api` container, which
is bind-mounted to `./data/StudyHub.db` on the host (see `docker-compose.yml`). Because the database
lives outside the container's writable layer, it **survives container recreation** (`docker compose
down` / `docker compose up` again, or `docker compose up --build` after a code change).

To stop the container without losing data:

```bash
docker compose down
```

### Resetting the database

The database is just a file. To reset it, stop the container and delete the file (or the whole
`data/` folder) on the host, then start the container again — it will be recreated with all
migrations applied:

```bash
docker compose down
rm -rf ./data
docker compose up -d
```

For a local (non-Docker) run, delete `src/UI/StudyHub.Api/App_Data/StudyHub.db` instead.

## Migrations

Migrations live in `src/Data/StudyHub.Data/Migrations`. `StudyHub.Data` holds the `DbContext`;
`StudyHub.Api` is the startup project used to resolve configuration/services for the EF Core
tooling.

Install the EF Core CLI tool once (if not already installed):

```bash
dotnet tool install --global dotnet-ef
```

Create a new migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Data/StudyHub.Data/StudyHub.Data.csproj \
  --startup-project src/UI/StudyHub.Api/StudyHub.Api.csproj
```

Apply migrations manually (normally not needed — the app applies them automatically on startup):

```bash
dotnet ef database update \
  --project src/Data/StudyHub.Data/StudyHub.Data.csproj \
  --startup-project src/UI/StudyHub.Api/StudyHub.Api.csproj
```

Per [`CLAUDE.md`](./CLAUDE.md), keep one migration per feature.

---

## Roadmap

Planned development phases, roughly in order:

1. **Foundation** — solution scaffold, layered architecture, base tooling. *(in progress)*
2. **Semester Management** — create and manage semesters.
3. **Course Management** — add and organize courses within a semester.
4. **Dashboard** — overview of courses, plans, and upcoming work.
5. **Document Library** — upload, organize, and browse study documents.
6. **Markdown Notes** — note-taking tied to courses and documents.
7. **Study Sessions** — plan and track study time.
8. **Calendar** — visualize sessions, deadlines, and course schedules.
9. **AI Features** — summaries, explanations, and review questions generated from study material.
10. **Flashcards** — decks with spaced repetition, AI generation from notes, Anki import/export.
11. **Analytics** — learning analytics and progress dashboards.

This roadmap describes intent and ordering, not committed dates.

---

## Contributing

StudyHub currently follows a simple, single-maintainer workflow:

- One feature per branch.
- Small, focused commits (a few files at a time).
- Descriptive commit messages.

Before contributing, read [`CLAUDE.md`](./CLAUDE.md) — it is the primary source of truth for the
architecture, layering rules, coding standards, naming conventions, and testing approach used in
this repository. Changes should preserve the Composite Component Architecture and the
contracts-first principle described there.

---

## License

*License to be determined.* A `LICENSE` file will be added once a license has been chosen for
this project.
