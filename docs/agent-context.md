# Agent Context

This file provides non-normative project context for agents.
Use it to understand repository layout, domain language, and naming intent for the AppLauncher codebase.
Normative enforcement remains in project instructions and `docs/agent-rule-catalog.md` when present.

## Quick Repository Map

- `src/`: production code and test projects.
- `src/Frontend/`: Windows desktop frontend for browsing and launching applications.
- `src/Backend/`: ASP.NET Core API host for application and group management.
- `src/*Contract/`: shared contracts, DTOs, and interfaces used across layers.
- `src/DataClasses/`: core data entities.
- `src/DataStoring/`: EF Core persistence, repositories, and migrations.
- `src/Mappings/`: dependency-injection composition and telemetry registration.
- `ci/`: container and CI-related assets.
- `docs/`: agent-facing context and project documentation.
- `Polipol.AppLauncher.sln`: solution entry point.

## Domain Invariants and Glossary

The application catalog distinguishes applications from access groups. Code, names, and DTO shapes should keep those concepts separate.

- Applications are launchable items with a file path, title, description, and optional icon data.
- Groups represent identity/access groups, identified externally by SID-style values.
- Application-group relations determine which groups can see or access an application.
- The frontend filters visible applications by the current Windows user's group context.

- `Application`: launchable entry shown by the AppLauncher UI.
- `Group`: identity/access group associated with one or more applications.
- `ApplicationGroup`: relation between an application and a group.
- `ApplicationDto`, `ApplicationCreateDto`, `ApplicationUpdateDto`: API/domain transfer shapes for application data.
- `GroupDto`, `GroupCreateDto`: API/domain transfer shapes for group data.
- `ApplicationAccessor`: frontend-facing backend client for retrieving applications.
- `WindowsUserContextAccessor`: integration with the Windows user/group context.
- `ApplicationOrchestrator`, `GroupOrchestrator`, `ApplicationGroupOrchestrator`: workflow services that sequence use cases.

## Technology Snapshot

- Runtime: .NET 10.
- Backend: ASP.NET Core Controllers with Swagger/OpenAPI in development.
- Frontend: WPF (`net10.0-windows`) using XAML views, view models, user controls, and `.resx` resources.
- Data: EF Core with PostgreSQL provider and code-first migrations.
- Configuration: `appsettings.json` files plus shared configuration DTOs and JSON parser contracts.
- Observability: OpenTelemetry logging, metrics, tracing, and OTLP exporter registration.
- Tests: MSTest with FluentAssertions for application-management behavior.

## Architecture and API Notes

- Contract projects (`*.Contract`) define public interfaces and DTOs. Prefer adding shared abstractions there before implementing them in the matching non-contract project.
- `DataClasses` contains the entity model. Keep persistence concerns in `DataStoring` and API/request concerns in `Backend`.
- Repositories live behind interfaces in `src/DataStoring.Contract/Repositories/` and are implemented under `src/DataStoring/Repositories/`.
- Workflow/orchestrator classes coordinate use cases through repository and management interfaces. Keep orchestration sequencing separate from mapper/filter implementation details.
- `ApplicationManagement` contains mappers, filter processing, and utility services such as byte-array conversion.
- `Mappings/ServiceCollectionFactory.cs` is the dependency-injection composition point for frontend and backend services. Update it when adding or replacing service implementations.
- `Backend/Program.cs` configures controllers, Swagger/OpenAPI, ProblemDetails, DI registration, automatic EF migration, HTTPS redirection, and controller mapping.
- Backend API routes are organized around `api/applications`, `api/applications/{applicationId}/groups`, and `api/groups`.
- Frontend UI composition lives in `MainWindow*`, `ApplicationDisplayModel`, `InfoBarModel`, and `UserControls/`. Localized/shared strings live in `SharedResources*.resx`.

## Naming Reference

### Project and Component Naming

- Project and namespace names use the existing `Polipol.AppStore` naming even though the repository is AppLauncher-oriented.
- Contract projects end with `.Contract` and should contain interfaces and DTOs, not concrete infrastructure implementations.
- Test projects use a `Tests` suffix.

### Class Suffix Targets

- `Accessor`: wraps retrieval from another source, usually the backend API or environment/user context.
- `Controller`: ASP.NET Core API endpoint class.
- `Dto`: data-transfer shape crossing layer or process boundaries.
- `Mapper`: field-to-field mapping between entities and DTOs.
- `Orchestrator`: workflow sequencing across repositories and domain services.
- `Processor`: in-process transformation/filtering logic.
- `Repository`: persistence contract or EF Core-backed CRUD implementation.
- `Factory`: object/service construction or dependency-registration helper.
- `Converter`: type/content conversion with minimal domain decision logic.
- `Model` / `ViewModel`: frontend presentation state.

### DTO and Request Naming

- Contract DTOs use `Dto` or purpose-specific variants such as `CreateDto` and `UpdateDto`.
- Backend form/request models use `RequestDto` where they represent HTTP-specific request shapes, especially multipart/form-data with icon uploads.
- Keep HTTP-specific upload abstractions (`IFormFile`, content type handling) in the backend layer and map them into contract DTOs before entering workflow/domain logic.

## C# Convention Snapshot

- Prefer file-scoped namespaces and single-line `using` directives.
- Prefer pattern matching for null checks (`is null`, `is not null`) where it improves clarity.
- Use `nameof` instead of string literals for member-name references.
- Trust nullability annotations and avoid redundant null checks that contradict the type system.
- Keep brace style consistent with surrounding code: no braces for simple single-line statements, braces for multi-line statements.
- Do not put `try`/`catch` blocks around imports/usings.

## Testing Snapshot

- Run the solution tests with `dotnet test Polipol.AppLauncher.sln` from the repository root.
- `src/ApplicationManagementTests/` currently covers filtering, byte-array conversion, and mapper behavior.
- Add tests close to the project being changed when behavior is added or fixed.
- If EF Core entities or repository behavior changes, consider whether a migration under `src/DataStoring/Migrations/` and persistence-focused tests are required.

## Local Run Commands

| Task | Command |
|---|---|
| Build solution | `dotnet build Polipol.AppLauncher.sln` |
| Run tests | `dotnet test Polipol.AppLauncher.sln` |
| Run backend | `dotnet run --project src/Backend/Backend.csproj` |
| Run frontend on Windows | `dotnet run --project src/Frontend/Frontend.csproj` |

## Change Placement Guide

- New API endpoint: start in `src/Backend/Controllers/`, add request DTOs in `src/Backend/Dtos/` only if the shape is HTTP-specific, and move shared shapes to `src/Configuration.Contract/Dtos/` or another relevant contract project.
- New business use case: add or extend an orchestrator contract in `src/ApplicationWorkflow.Contract/`, implement sequencing in `src/ApplicationWorkflow/`, and register it in `src/Mappings/ServiceCollectionFactory.cs`.
- New mapping/filtering behavior: add contracts in `src/ApplicationManagement.Contract/`, implementations in `src/ApplicationManagement/`, and tests in `src/ApplicationManagementTests/`.
- New persistence behavior: add repository contracts in `src/DataStoring.Contract/Repositories/`, implementations in `src/DataStoring/Repositories/`, and update `AppDbContext` or migrations when the entity model changes.
- New frontend display behavior: update the relevant XAML/view model/user control under `src/Frontend/`, and check `SharedResources*.resx` for user-facing strings.
