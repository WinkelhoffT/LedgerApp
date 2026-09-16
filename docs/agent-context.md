# Agent Context

This file provides non-normative project context for agents.
Use it to understand repository layout, domain language, and naming intent for the StudyHub codebase.
Normative enforcement remains in `CLAUDE.md` and `docs/agent-rule-catalog.md`.

## Quick Repository Map

- `src/Shared/StudyHub.Shared/<Domain>/`: DTOs, request types, error-code classes, and exceptions
  that cross the `StudyHub.Api` ↔ `StudyHub.Logic.Integration` ↔ `StudyHub.Logic.Business`
  boundary (`CourseDto`, `CreateCourseRequest`, `CourseErrorCodes`, `CourseNotFoundException`,
  `CourseArchivedException`, …) — see `CLAUDE.md`, "Contracts First". Also the intended home for
  future Options/configuration classes.
- `src/Data/StudyHub.Data`: `ApplicationDbContext`, EF Core migrations, and the repository
  implementations (`CourseRepository`, `SemesterRepository`, `DocumentRepository`) plus their DI
  registration (`ServiceCollectionExtensions.AddStudyHubDataRepositories`).
- `src/Logic/StudyHub.Logic.Domain`: domain entities (`Course`, `Semester`, `Document`,
  `SemesterProgress`), repository contracts (`ICourseRepository`, `ISemesterRepository`,
  `IDocumentRepository`), and domain services that encode a business rule too specific to inline
  in an orchestrator (`IActiveSemesterProvider`/`ActiveSemesterProvider` — which semester counts
  as "active" right now).
- `src/Logic/StudyHub.Logic.Business`: use-case orchestration per domain (`CourseOrchestrator`,
  `SemesterOrchestrator`, `DocumentOrchestrator`, `DashboardOrchestrator`), each with a
  `Contracts/` subfolder holding its own Business-facing interface (`ICourseOrchestrator`, …) —
  these interfaces are Business-local, unlike the `Shared` DTOs/exceptions they use. Orchestrators
  coordinate Domain repositories/services and assemble DTOs; they must not embed business rules
  themselves.
- `src/Logic/StudyHub.Logic.Integration/<Domain>/`: Accessor classes `StudyHub.UI` uses to call
  `StudyHub.Api` (`ISemesterAccessor`, `ICourseAccessor`, `IDocumentAccessor`,
  `IDashboardAccessor`), plus the project's own `ServiceCollectionExtensions.AddStudyHubIntegration`
  for HttpClient/DI registration. Depends only on `StudyHub.Shared` — never on `Logic.Business` or
  `Logic.Domain` (LAY-7 in `docs/agent-rule-catalog.md`). Also the intended home for future AI
  provider/external-service adapters.
- `src/Infrastructure/StudyHub.Infrastructure`: currently empty (just a no-op
  `ServiceCollectionExtensions`) — reserved for genuine external-infrastructure concerns (file
  storage, email) that don't exist yet. Repositories used to live here; they moved to
  `StudyHub.Data`.
- `src/UI/StudyHub.Api`: ASP.NET Core backend host. Controllers (`SemesterController`,
  `CourseController`, `DocumentController`, `DashboardController`) call a Business orchestrator;
  `*ExceptionHandler` classes map Business/Domain exceptions to `ProblemDetails`.
- `src/UI/StudyHub.UI`: Blazor Web App frontend — `Components/Pages`, `Components/Layout`,
  `Components/Shared`, every component paired with a `.razor.cs` code-behind. Depends only on
  `StudyHub.Shared` and `StudyHub.Logic.Integration` (never `Logic.Business`/`Logic.Domain`
  directly). Cross-page UI helpers live under `Services/`.
- `tests/StudyHub.Tests`: xUnit tests, mirroring the production layout
  (`Api/`, `Data/`, `Logic/Business/`, `Logic/Domain/`).
- `docs/`: agent-facing context and project documentation.
  `docs/mockup/StudyHub.html` is a static HTML/JS mockup of every page's intended UI — the
  reference for new pages before they're implemented. `docs/plans/` holds feature implementation
  plans (Medium/Large classification per `CLAUDE.md`).
- `agents/`: task templates for a multi-agent orchestration workflow (architect, orchestrator,
  consolidator, implementer, reviewer, verification).
- `StudyHub.slnx`: solution entry point.

## Domain Glossary

StudyHub centralizes a computer science student's study material and planning around semesters
and courses.

- `Semester`: a time-boxed study period (e.g. one academic term) that owns `Course`s and tracks
  overall progress.
- `Course`: belongs to a `Semester`; the unit courses/documents are organized around.
- `Document`: an uploaded file (course material, script, notes) associated with **either** a
  `Course` or a `Semester` — never both, never neither (enforced in the `Document` domain
  constructor and, per `review.md` §3, intended as a DB check constraint).
- `Note`: a Markdown note following the same "exactly one of Course or Semester" ownership pattern
  as `Document` (see `docs/plans/markdown-notes-plan.md`).
- `SemesterProgress`: a computed value describing how far along a semester is, produced by
  `ISemesterProgressCalculator`/`SemesterProgressCalculator`.
- Soft delete: `Course`/`Semester`/`Document`/`Note` use an `IsArchived` flag with
  `Archive()`/`Restore()` domain methods and a dedicated `*ArchivedException`, rather than hard
  deletes. Foreign keys use `DeleteBehavior.Restrict` for exactly this reason.

## Technology Snapshot

- Runtime: .NET 10.
- Backend: ASP.NET Core Controllers in `StudyHub.Api`.
- Frontend: Blazor Web App (`StudyHub.UI`), Bootstrap for styling.
- Data: EF Core, code-first migrations, SQLite provider (`Microsoft.Data.Sqlite` /
  `UseSqlite`).
- Tests: xUnit + Moq (Business layer), EF Core `UseInMemoryDatabase` (Data-layer tests),
  `WebApplicationFactory<Program>` (Api layer).

## Architecture and API Notes

- Follow the Composite Component layering described in `CLAUDE.md` ("Architecture") and
  `docs/architecture.md`: `Shared` / `Data` / `Logic{Domain, Business, Integration}` /
  `Infrastructure` / `UI` / `Tests`.
- End-to-end flow: `StudyHub.UI` (Razor + code-behind) → Accessor (`Logic.Integration/<Domain>/`)
  → Controller (`StudyHub.Api/<Domain>/`) → `*Orchestrator`
  (`Logic.Business/<Domain>/`) → Domain entity/service (`Logic.Domain/<Domain>/`) + Repository
  (`Data/<Domain>/`).
- Business-local contracts (`ICourseOrchestrator`, `ISemesterOrchestrator`, `IDocumentOrchestrator`,
  `IDashboardOrchestrator`) live in `StudyHub.Logic.Business/<Domain>/Contracts/`, separate from the
  `*Orchestrator` implementation. `DashboardOrchestrator` is the example to follow when a use case
  needs a business rule beyond plain CRUD: it depends on `ISemesterRepository` +
  `IActiveSemesterProvider` (Domain) + `ISemesterProgressCalculator` (Domain) and only assembles
  the result DTO itself — the "which semester is active" rule lives in
  `Logic.Domain.Semesters.ActiveSemesterProvider`, not inline in the orchestrator.
- Repository contracts live in `StudyHub.Logic.Domain/<Domain>/I<Domain>Repository.cs`;
  implementations live in `StudyHub.Data/<Domain>/<Domain>Repository.cs`.
- `StudyHub.Data`/`StudyHub.Infrastructure`/`StudyHub.Logic.Business`/`StudyHub.Logic.Integration`/
  `StudyHub.Api`/`StudyHub.UI` each carry their own `ServiceCollectionExtensions` for DI
  registration, composed from the relevant host's `Program.cs` (`StudyHub.Api` and `StudyHub.UI`
  are the two composition roots).
- Frontend Accessors (`SemesterAccessor`, `CourseAccessor`, `DocumentAccessor`,
  `DashboardAccessor`, all in `Logic.Integration/<Domain>/`) are narrow and purpose-specific —
  shaped around what a page actually calls, not a 1:1 mirror of the Business interface. E.g.
  `ISemesterAccessor` has 5 methods where `ISemesterOrchestrator` has 6 (no `GetByIdAsync`, unused
  by any page).
- UI cross-page helpers live in `StudyHub.UI/Services/` (`ThemeAccessor`+`ThemeStateHolder`,
  `SidebarStateHolder`, `PageHeaderStateHolder`) — role-based names, not the generic `Service`
  suffix. `ThemeStateHolder` depends on `IThemeAccessor` for the underlying JS-interop calls,
  keeping raw data access separate from state-holding/notification logic.

## Naming Reference

### Project and Component Naming

- Project and namespace names use the `StudyHub` prefix (`StudyHub.Shared`, `StudyHub.Data`,
  `StudyHub.Logic.Domain`, `StudyHub.Logic.Business`, `StudyHub.Logic.Integration`,
  `StudyHub.Infrastructure`, `StudyHub.Api`, `StudyHub.UI`, `StudyHub.Tests`).
- Test project uses a `Tests` suffix (`StudyHub.Tests`, a single project mirroring the production
  layout by folder rather than one test project per component).

### Class Suffix Targets

- `Orchestrator`: Business-layer use-case coordination for a domain (`CourseOrchestrator`,
  `SemesterOrchestrator`, `DocumentOrchestrator`, `DashboardOrchestrator`) — coordinates Domain
  repositories/services and assembles DTOs; never embeds business rules itself.
- `Provider`: a Domain-layer service exposing a piece of domain data resolved by a business rule
  (`IActiveSemesterProvider`/`ActiveSemesterProvider`).
- `Repository`: persistence contract (`Logic.Domain`) or EF Core-backed CRUD implementation
  (`StudyHub.Data`).
- `Controller`: ASP.NET Core API endpoint class in `StudyHub.Api` (`SemesterController`, …).
- `ExceptionHandler`: maps domain/business exceptions to HTTP problem responses for one domain's
  endpoints (`CourseExceptionHandler`, `SemesterExceptionHandler`, `DocumentExceptionHandler`).
- `Accessor`: narrow, purpose-specific HTTP access role, used both for `Logic.Integration`'s
  Api-calling classes (`SemesterAccessor`, …) and for a UI-local JS-interop wrapper
  (`ThemeAccessor`).
- `StateHolder`: holds in-memory UI state and raises a `Changed` event; no raw data access
  (`PageHeaderStateHolder`, `SidebarStateHolder`, `ThemeStateHolder`).
- `Dto`: data-transfer shape crossing the API/Integration/Business boundary (`CourseDto`,
  `SemesterDto`, `DocumentDto`, `SemesterProgressDto`, `DocumentContentDto`) — lives in
  `StudyHub.Shared`.
- `Request`: input shape for a specific use case (`CreateCourseRequest`, `UpdateSemesterRequest`,
  `UploadDocumentRequest`) — lives in `StudyHub.Shared`.
- `ErrorCodes`: static class of error-code constants for one domain (`CourseErrorCodes`,
  `SemesterErrorCodes`, `DocumentErrorCodes`) — lives in `StudyHub.Shared`.
- `Service`: generic bucket suffix — not used anywhere in the codebase; do not introduce it for
  new classes (see `CLAUDE.md`, "UI Services and Frontend Integration").

### DTO and Request Naming

- DTOs use the `Dto` suffix; requests use `Create<Domain>Request` / `Update<Domain>Request` /
  `Upload<Domain>Request` naming. Both live in `StudyHub.Shared/<Domain>/`.
- Keep HTTP-specific request/response shapes (e.g. `IFormFile` binding) in the API layer and map
  them into `Shared` request DTOs before entering Business/Domain logic.

## C# Convention Snapshot

- Prefer file-scoped namespaces.
- Nullable reference types are enabled (`<Nullable>enable</Nullable>`) across production projects;
  trust nullability annotations and avoid redundant null checks that contradict the type system.
- New entity identifiers use `Guid.NewGuid()` (see `Course`, `Semester`, `Document` constructors).
- Domain invariants are enforced in entity constructors/methods, raising a dedicated
  `*ValidationException` / `*ArchivedException` / `*NotFoundException` per domain (defined in
  `StudyHub.Shared`, since Accessors need to reconstruct them from `ProblemDetails`).

## Testing Snapshot

- Run tests with `dotnet test` from the repository root (`StudyHub.slnx`).
- `tests/StudyHub.Tests` mirrors the production namespace layout: `Api/<Domain>`, `Data/<Domain>`,
  `Logic/Business/<Domain>`, `Logic/Domain/<Domain>`.
- Add tests close to the layer being changed: Business-logic behavior in
  `Logic/Business/<Domain>`, domain invariants in `Logic/Domain/<Domain>`, repository behavior in
  `Data/<Domain>`, controller behavior in `Api/<Domain>`.
- If EF Core entities or repository behavior changes, add a migration under
  `src/Data/StudyHub.Data/Migrations/` (one migration per feature, per `CLAUDE.md`).

## Local Run Commands

| Task | Command |
|---|---|
| Build solution | `dotnet build StudyHub.slnx` |
| Run tests | `dotnet test StudyHub.slnx` |
| Run backend API | `dotnet run --project src/UI/StudyHub.Api/StudyHub.Api.csproj` |
| Run Blazor UI | `dotnet run --project src/UI/StudyHub.UI/StudyHub.UI.csproj` |

## Change Placement Guide

- New API endpoint: add an action to a `<Domain>Controller` in
  `src/UI/StudyHub.Api/<Domain>/`, calling a Business orchestrator (see `CLAUDE.md`, "API Layer").
- New business use case: extend the relevant `I<Domain>Orchestrator`/`<Domain>Orchestrator` in
  `src/Logic/StudyHub.Logic.Business/<Domain>/`, add a `Request`/`Dto` type in
  `src/Shared/StudyHub.Shared/<Domain>/` as needed, and register/adjust DI in
  `src/Logic/StudyHub.Logic.Business/ServiceCollectionExtensions.cs`.
- New domain rule/entity: add to `src/Logic/StudyHub.Logic.Domain/<Domain>/`, keep it free of
  external dependencies (besides `StudyHub.Shared` for any exception types it throws), and extend
  the matching `I<Domain>Repository` contract if persistence is affected.
- New persistence behavior: extend the repository in
  `src/Data/StudyHub.Data/<Domain>/<Domain>Repository.cs`, update
  `ApplicationDbContext`/`IEntityTypeConfiguration`, and add an EF Core migration under
  `src/Data/StudyHub.Data/Migrations/`.
- New frontend display behavior: update the relevant page under
  `src/UI/StudyHub.UI/Components/Pages/` with a matching `.razor.cs` code-behind, and extend the
  domain's Accessor in `src/Logic/StudyHub.Logic.Integration/<Domain>/` if it needs a new
  operation. Check `docs/mockup/StudyHub.html` for the intended layout before implementing a new
  page.
- New Api/Integration/Business-crossing DTO, request, error code, or exception: place it in
  `src/Shared/StudyHub.Shared/<Domain>/` (see `CLAUDE.md`, "Contracts First") — not in
  `StudyHub.Logic.Business`/`StudyHub.Logic.Domain`, since `Logic.Integration` cannot depend on
  either.
