# Agent Context

This file provides non-normative project context for agents.
Use it to understand repository layout, domain language, and naming intent for the StudyHub codebase.
Normative enforcement remains in `CLAUDE.md` and `docs/agent-rule-catalog.md`.

## Quick Repository Map

- `src/Shared/StudyHub.Shared`: cross-cutting DTOs, exceptions, and options/configuration classes
  genuinely shared across multiple layers/components.
- `src/Data/StudyHub.Data`: `ApplicationDbContext`, EF Core migrations, and (target state)
  repository implementations and `IEntityTypeConfiguration` classes.
- `src/Logic/StudyHub.Logic.Domain`: domain entities (`Course`, `Semester`, `Document`,
  `SemesterProgress`), domain exceptions, and repository contracts (`ICourseRepository`,
  `ISemesterRepository`, `IDocumentRepository`).
- `src/Logic/StudyHub.Logic.Business`: use-case orchestration per domain (`CourseManagement`,
  `SemesterManagement`, `DocumentManagement`, `DashboardManagement`), their DTOs, request types,
  and business-level exceptions/error codes.
- `src/Infrastructure/StudyHub.Infrastructure`: currently hosts the EF Core repository
  implementations (`CourseRepository`, `SemesterRepository`, `DocumentRepository`); target state
  (see `review.md`) moves these into `StudyHub.Data` and reserves `Infrastructure` for genuine
  external-infrastructure concerns (file storage, email, AI provider adapters).
- `src/UI/StudyHub.Api`: ASP.NET Core backend host. Currently Minimal API endpoint groups
  (`*Endpoints.cs`) calling Business contracts directly; target state (see `CLAUDE.md`, "API
  Layer") is Controllers calling a Business-layer orchestrator.
- `src/UI/StudyHub.UI`: Blazor Web App frontend — `Components/Pages`, `Components/Layout`,
  per-domain API clients (`SemesterApiClient`, `CourseApiClient`, `DocumentApiClient`,
  `DashboardApiClient`), and cross-page UI helpers under `Services/`.
- `tests/StudyHub.Tests`: xUnit tests, mirroring the production layout
  (`Api/`, `Infrastructure/`, `Logic/Business/`, `Logic/Domain/`).
- `docs/`: agent-facing context and project documentation.
- `agents/`: task templates for a multi-agent orchestration workflow (architect, orchestrator,
  consolidator, implementer, reviewer, verification).
- `StudyHub.slnx`: solution entry point.

## Domain Invariants and Glossary

StudyHub centralizes a computer science student's study material and planning around semesters
and courses.

- `Semester`: a time-boxed study period (e.g. one academic term) that owns `Course`s and tracks
  overall progress.
- `Course`: belongs to a `Semester`; the unit courses/documents are organized around.
- `Document`: an uploaded file (course material, script, notes) associated with **either** a
  `Course` or a `Semester` — never both, never neither (enforced in the `Document` domain
  constructor and, per `review.md` §3, intended as a DB check constraint).
- `SemesterProgress`: a computed value describing how far along a semester is, produced by
  `ISemesterProgressCalculator`/`SemesterProgressCalculator`.
- Soft delete: `Course`/`Semester` (and `Document`, per the document-management plan) use an
  `IsArchived` flag with `Archive()`/`Restore()` domain methods and a dedicated
  `*ArchivedException`, rather than hard deletes.

## Technology Snapshot

- Runtime: .NET 10.
- Backend: ASP.NET Core, currently Minimal API endpoint groups in `StudyHub.Api`.
- Frontend: Blazor Web App (`StudyHub.UI`), Bootstrap for styling.
- Data: EF Core, code-first migrations, SQLite provider (`Microsoft.Data.Sqlite` /
  `UseSqlite`).
- Tests: xUnit.

## Architecture and API Notes

- Follow the Composite Component layering described in `CLAUDE.md` ("Architecture") and
  `docs/architecture.md`: `Shared` / `Data` / `Logic{Domain, Business, Integration}` /
  `Infrastructure` / `UI` / `Tests`.
- Business contracts (`ICourseManagement`, `ISemesterManagement`, `IDocumentManagement`,
  `IDashboardManagement`) live in `StudyHub.Logic.Business` next to their DTOs and implementation;
  target state (per `CLAUDE.md`, "Contracts First") separates contract types (interfaces, DTOs,
  exceptions) from implementation, e.g. via a `Contracts/` subfolder per domain.
  today (per `review.md`, gap 1.2) they are unstructured in the same folder as the implementation.
- Repository contracts live in `StudyHub.Logic.Domain/<Domain>/I<Domain>Repository.cs`;
  implementations are currently in `StudyHub.Infrastructure/<Domain>/<Domain>Repository.cs`
  (target: `StudyHub.Data`, see `CLAUDE.md`, "Entity Framework").
- `StudyHub.Api`/`StudyHub.Infrastructure`/`StudyHub.Logic.Business`/`StudyHub.UI` each carry a
  `ServiceCollectionExtensions` class for their own DI registration, composed from the relevant
  host's `Program.cs`.
- Frontend API clients (`SemesterApiClient`, `CourseApiClient`, `DocumentApiClient`,
  `DashboardApiClient`) currently implement the full matching Business interface over HTTP; target
  state (per `CLAUDE.md`, "UI Services and Frontend Integration") is small, purpose-specific
  Accessor classes instead of one class per domain covering the whole contract.
- UI cross-page helpers live in `StudyHub.UI/Services/` (`ThemeService`, `SidebarStateService`,
  `PageHeaderService`); target state renames these away from the generic `Service` suffix
  (`Accessor`, `Provider`, `StateHolder`, `Formatter`, …) and splits data access from
  state/notification logic where a class currently does both (see `CLAUDE.md`, "UI Services and
  Frontend Integration").

## Naming Reference

### Project and Component Naming

- Project and namespace names use the `StudyHub` prefix (`StudyHub.Shared`, `StudyHub.Data`,
  `StudyHub.Logic.Domain`, `StudyHub.Logic.Business`, `StudyHub.Infrastructure`, `StudyHub.Api`,
  `StudyHub.UI`, `StudyHub.Tests`).
- Test project uses a `Tests` suffix (`StudyHub.Tests`, a single project mirroring the production
  layout by folder rather than one test project per component).

### Class Suffix Targets

- `Management`: Business-layer use-case orchestration for a domain (`CourseManagement`,
  `SemesterManagement`, `DocumentManagement`, `DashboardManagement`).
- `Repository`: persistence contract (Domain layer) or EF Core-backed CRUD implementation
  (Infrastructure today, Data at target state).
- `Endpoints`: current Minimal API route-group registration class (`CourseEndpoints`, …); target
  state replaces these with `Controller` classes (see `CLAUDE.md`, "API Layer").
- `Controller`: target-state ASP.NET Core API endpoint class (not yet used in the codebase).
- `ExceptionHandler`: maps domain/business exceptions to HTTP problem responses for one domain's
  endpoints (`CourseExceptionHandler`, `SemesterExceptionHandler`, `DocumentExceptionHandler`).
- `ApiClient`: current frontend HTTP client implementing a full Business contract; target state
  replaces these with narrower `Accessor` classes (see above).
- `Accessor`: target-state role for narrow, purpose-specific frontend HTTP access.
- `Dto`: data-transfer shape crossing the API/frontend boundary (`CourseDto`, `SemesterDto`,
  `DocumentDto`, `SemesterProgressDto`, `DocumentContentDto`).
- `Request`: business-layer input shape for a specific use case (`CreateCourseRequest`,
  `UpdateSemesterRequest`, `UploadDocumentRequest`).
- `ErrorCodes`: static class of business error-code constants for one domain (`CourseErrorCodes`,
  `SemesterErrorCodes`, `DocumentErrorCodes`).
- `Service`: generic bucket suffix currently used under `StudyHub.UI/Services/`; not to be used
  for new classes (see `CLAUDE.md`, "UI Services and Frontend Integration").

### DTO and Request Naming

- Business DTOs use the `Dto` suffix.
- Business use-case inputs use `Create<Domain>Request` / `Update<Domain>Request` /
  `Upload<Domain>Request` naming.
- Keep HTTP-specific request/response shapes in the API layer and map them into Business request
  DTOs before entering Business/Domain logic.

## C# Convention Snapshot

- Prefer file-scoped namespaces.
- Nullable reference types are enabled (`<Nullable>enable</Nullable>`) across production projects;
  trust nullability annotations and avoid redundant null checks that contradict the type system.
- New entity identifiers use `Guid.NewGuid()` (see `Course`, `Semester`, `Document` constructors).
- Domain invariants are enforced in entity constructors/methods, raising a dedicated
  `*ValidationException` / `*ArchivedException` / `*NotFoundException` per domain.

## Testing Snapshot

- Run tests with `dotnet test` from the repository root (`StudyHub.slnx`).
- `tests/StudyHub.Tests` mirrors the production namespace layout: `Api/<Domain>`,
  `Infrastructure/<Domain>`, `Logic/Business/<Domain>`, `Logic/Domain/<Domain>`.
- Add tests close to the layer being changed: Business-logic behavior in
  `Logic/Business/<Domain>`, domain invariants in `Logic/Domain/<Domain>`, repository behavior in
  `Infrastructure/<Domain>` (target: `Data/<Domain>`), endpoint/controller behavior in
  `Api/<Domain>`.
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

- New API endpoint: current state — add a Minimal API route in
  `src/UI/StudyHub.Api/<Domain>/<Domain>Endpoints.cs`; target state — add an action to a
  `<Domain>Controller` in the same folder, calling a Business orchestrator (see `CLAUDE.md`,
  "API Layer").
- New business use case: extend the relevant `I<Domain>Management`/`<Domain>Management` in
  `src/Logic/StudyHub.Logic.Business/<Domain>/`, add a `Request`/`Dto` type as needed, and
  register/adjust DI in `src/Logic/StudyHub.Logic.Business/ServiceCollectionExtensions.cs`.
- New domain rule/entity: add to `src/Logic/StudyHub.Logic.Domain/<Domain>/`, keep it free of
  external dependencies, and extend the matching `I<Domain>Repository` contract if persistence is
  affected.
- New persistence behavior: extend the repository in
  `src/Infrastructure/StudyHub.Infrastructure/<Domain>/<Domain>Repository.cs` (target:
  `src/Data/StudyHub.Data/<Domain>/`), update `ApplicationDbContext`/`IEntityTypeConfiguration`,
  and add an EF Core migration under `src/Data/StudyHub.Data/Migrations/`.
- New frontend display behavior: update the relevant page under
  `src/UI/StudyHub.UI/Components/Pages/` with a matching `.razor.cs` code-behind, and extend the
  domain's API client/accessor in `src/UI/StudyHub.UI/<Domain>/`.
- Cross-layer shared DTO/exception/options type: place it in `src/Shared/StudyHub.Shared/`
  instead of `StudyHub.Logic.Business`/`StudyHub.Logic.Domain` (see `CLAUDE.md`, "Contracts
  First").
