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
architecture, running on the default Blazor Web App template. Feature work (authentication,
persistence, course/study management, UI, AI features) has not started yet — see
[Features](#features) and [Roadmap](#roadmap) below for what's implemented versus planned.

---

## Features

### Current Features

- Solution scaffold following the target layered architecture (Shared, Data, Logic, Infrastructure,
  UI, Tests projects, wired via project references).
- Blazor Web App host project (Interactive Server render mode) running on .NET 10.

No user-facing product features (authentication, course management, study planning, etc.) have
been implemented yet.

### Planned Features

- ASP.NET Core Identity–based authentication and user accounts.
- Semester and course management.
- Study planning and study session tracking.
- Document management and a document library.
- Markdown-based notes.
- Calendar integration for study sessions and deadlines.

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
- **ASP.NET Core Identity** *(planned)*
- **Entity Framework Core** *(planned)* — Code First
- **SQL Server** *(planned)* — primary target database
- **MudBlazor** *(planned)* — component library for the UI layer
- **xUnit** *(planned)* — unit testing
- **FluentValidation** *(planned)*
- **Mapster** *(planned)*

Items marked *(planned)* are part of the target stack described in [`CLAUDE.md`](./CLAUDE.md) but
are not yet present in the codebase.

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

**UI**
Presentation only, built with Blazor. Razor components stay thin and call into the Business layer
through contracts; no business logic lives here. Backend/API endpoints are considered part of
this layer.

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
│   │   └── StudyHub.Data/              # EF Core models & persistence
│   ├── Logic/
│   │   ├── StudyHub.Logic.Domain/      # Core business models & rules
│   │   ├── StudyHub.Logic.Integration/ # External system adapters/clients
│   │   └── StudyHub.Logic.Business/    # Use-cases & orchestration
│   ├── Infrastructure/
│   │   └── StudyHub.Infrastructure/    # Implementations of external services
│   └── UI/
│       └── StudyHub.UI/                # Blazor Web App (presentation + API endpoints)
├── tests/
│   └── StudyHub.Tests/                 # Automated tests (xUnit)
├── docs/                               # Architecture and reference documentation
├── StudyHub.slnx                       # Solution file
└── CLAUDE.md                           # Architecture & coding guidelines
```

---

## Getting Started

> These steps reflect the current, minimal state of the project. Database setup and Identity
> configuration will be filled in as those pieces are implemented.

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (or SQL Server LocalDB) *(planned — not required yet, as persistence is not wired up)*

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

*Not yet applicable.* Entity Framework Core, migrations, and connection-string configuration have
not been added to the project yet. This section will be updated once the Data layer is
implemented.

### Run the application

```bash
dotnet run --project src/UI/StudyHub.UI/StudyHub.UI.csproj
```

### Run the tests

*Not yet applicable.* A dedicated test project will be added under `tests/` as business logic is
implemented, per the testing strategy in [`CLAUDE.md`](./CLAUDE.md). Once available, tests will
run with:

```bash
dotnet test
```

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
10. **Flashcards** — generate flashcards from notes/documents, with Anki export.
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
