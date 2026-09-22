# CLAUDE.md

# Project

This repository contains **StudyHub**.

StudyHub is a modular Blazor Web Application that serves as a personal learning companion for computer science students.

The goal is to centralize every aspect of studying into a single application, including:

- Semester management
- Course management
- Study planning
- Document management
- Markdown notes
- AI-assisted learning
- Flashcard generation
- Learning analytics

The application is intended both as a daily productivity tool and as a portfolio project demonstrating modern .NET software architecture.

---

# Documentation

Before implementing any feature, review the relevant documentation.

Core documentation:

- `CLAUDE.md` (this file)
- `docs/architecture.md`
- `docs/agent-context.md`
- `docs/agent-rule-catalog.md`
- `docs/plans/`

If documentation conflicts with the current implementation, report the inconsistency instead of making assumptions.

---

# Development Workflow

Every task should be classified before implementation.

## Small

Examples:

- UI improvements
- Bug fixes
- Small refactorings
- Validation changes

These may be implemented directly.

---

## Medium

Examples:

- New CRUD feature
- New domain object
- New page
- New business workflow

Before implementation:

1. Explain the planned architecture.
2. Describe affected layers.
3. Identify required contracts.
4. Explain database impact.

---

## Large

Examples:

- AI integration
- Authentication changes
- File management
- Search engine
- OCR
- Handwritten note import

Before implementation:

1. Propose an implementation plan.
2. Wait for confirmation before writing code.

---

# Architecture

The project follows a **Composite Component Architecture Pattern**.

> The codebase was migrated to the rules below (Controllers, Accessors, Razor code-behind,
> repositories in `Data`) domain by domain (Semester, Course, Document, Dashboard) following the
> gap analysis in `review.md`. Accessors live in `StudyHub.Logic.Integration`, not in `StudyHub.UI`
> itself — the UI depends on Integration's Accessor contracts, never talks to `StudyHub.Api` (an
> HTTP call to a separate process) directly. DTOs, requests, error codes, and exceptions that cross
> that Api/Integration/Business boundary live in `StudyHub.Shared`, since Integration must not
> depend on `Logic.Business`/`Logic.Domain` (see "Layering" below).
>
> Every project below that has an implementation (`Data`, `Logic.Domain`, `Logic.Business`,
> `Logic.Integration`) has a matching `.Contract` project (`Data.Contract`, `Logic.Domain.Contract`,
> `Logic.Business.Contract`, `Logic.Integration.Contract`) that holds only the interfaces
> (and, per component, DTOs/exceptions local to that interface) consumers need — never the
> implementation. Entities (`Note`, `Semester`, `Course`, `Document`, …) live in
> `StudyHub.Shared.Domain`, since they're consumed across Data, Domain and Business alike; repository
> interfaces (`INoteRepository`, …) live in `StudyHub.Data.Contract`, not `Logic.Domain`.

Projects:

- Shared (+ `Shared.Domain` for entities)
- Data (+ `Data.Contract` for repository interfaces)
- Logic (Domain, Business, Integration — each with a matching `.Contract` project)
- UI
- Infrastructure
- Tests

The Logic layer consists of:

- Domain
- Business
- Integration

Responsibilities:

## Domain

Contains:

- Domain Rules (e.g. `ActiveSemesterProvider`, `SemesterProgressCalculator`), whose interfaces
  live in `Logic.Domain.Contract`

Entities and Value Objects (`Note`, `Semester`, `Course`, `Document`, …) live in
`StudyHub.Shared.Domain`, not here — see "Architecture" above. `Logic.Domain` depends on
`Shared.Domain` and its own `Logic.Domain.Contract`; no other external dependencies.

---

## Business

Contains:

- Use Cases
- Application Workflows
- Orchestration
- Validation

Business coordinates Domain and Integration.

Business never accesses Infrastructure directly.

The Business-layer class that a Controller calls is named `<Domain>Orchestrator`
(`SemesterOrchestrator`, `CourseOrchestrator`, `DocumentOrchestrator`, `DashboardOrchestrator`),
not `<Domain>Management` — this project standardizes on `Orchestrator`, matching its role.
Orchestrators coordinate repositories/Domain services and assemble DTOs; they must not embed
business rules themselves (e.g. "which semester counts as active right now" is a domain rule, so
it belongs in a Domain-layer type the orchestrator calls, not inline in the orchestrator method).

---

## Integration

Contains:

- Accessors calling `StudyHub.Api` on behalf of `StudyHub.UI` (see "UI Services and Frontend
  Integration" below) — `StudyHub.Api` is a separate process, so from the UI's perspective calling
  it is external access like any other.
- AI Providers
- External Services
- Adapters

Integration must not depend on `Logic.Domain` or `Logic.Business` (per `docs/agent-rule-catalog.md`
rule LAY-7); it depends only on `Shared` for the DTOs/exceptions it needs. Business may depend on
Integration (e.g. to orchestrate an external call as part of a workflow), never the other way round.

---

# Contracts First

All communication between layers happens through contracts.

Rules:

- Never instantiate implementations directly.
- Depend only on interfaces.
- Infrastructure implements contracts.
- UI communicates only with Integration's Accessor contracts — never directly with `StudyHub.Api`,
  `Logic.Business`, or `Logic.Domain`.

Keep each component's contract (interfaces, and any DTOs/exceptions local to that contract)
physically separate from its implementation, in a sibling `<Project>.Contract` project — not a
`Contracts/` subfolder inside the implementation project. `Data`, `Logic.Domain`, `Logic.Business`,
and `Logic.Integration` each have a matching `.Contract` project (`Data.Contract`,
`Logic.Domain.Contract`, `Logic.Business.Contract`, `Logic.Integration.Contract`); the
implementation project references and implements its own `.Contract` project, and consumers
reference the `.Contract` project, never the implementation. This applies to component-local
contracts that only that component's own implementation exposes (e.g. `ISemesterOrchestrator` in
`Logic.Business.Contract`) — not to the wire-level DTOs/exceptions described below.

DTOs, request/error-code types, and exceptions that cross the `StudyHub.Api` ↔ `Logic.Integration`
↔ `Logic.Business` boundary (i.e. anything an Accessor constructs, throws, or catches) belong in
`Shared` (`StudyHub.Shared/<Domain>/`), because Integration must not depend on `Logic.Business` or
`Logic.Domain` (see "Integration" above). This includes both Business-level DTOs/requests
(`CourseDto`, `CreateCourseRequest`, …) and the Domain-level invariant exceptions entities throw
(`CourseArchivedException`, `CourseValidationException`, …) when an Accessor needs to reconstruct
them from a `ProblemDetails` error code. Options/configuration classes also belong in `Shared`
(e.g. under a `Configuration` area). Types genuinely local to one component's contract (e.g. a
Business orchestration interface in `Logic.Business.Contract`) stay in that component's `.Contract`
project — don't move everything to `Shared` on principle. Entities are the one exception: they
cross Data, Domain, and Business, so they live in `Shared.Domain` rather than any single component.

---

# API Layer

Use ASP.NET Core Controllers, not Minimal API endpoint groups, for `StudyHub.Api`. Controllers stay
thin (routing, model binding, status codes) and call an orchestrator from the Business layer —
never a repository or the Domain layer directly. Do not let a controller action embed workflow
logic; that belongs in the Business orchestrator it calls.

---

# UI Services and Frontend Integration

- `StudyHub.UI` never talks to `StudyHub.Api` directly. It depends on small, purpose-specific
  Accessor interfaces living in `StudyHub.Logic.Integration.Contract/<Domain>/` (implemented by
  the matching classes in `StudyHub.Logic.Integration/<Domain>/`), injected into Razor components
  like any other service. Avoid one "fat" accessor per aggregate that implements
  the full Business contract over HTTP (e.g. one class implementing `ISemesterOrchestrator`
  end-to-end); prefer narrower accessors that map to what a page actually needs. HttpClient/DI
  wiring for accessors lives in `Logic.Integration`'s own `ServiceCollectionExtensions`
  (`AddStudyHubIntegration`), called from `StudyHub.UI`'s `Program.cs` composition root.
- Name UI-local helper classes (JS interop wrappers, cross-page state) by role (`Accessor`,
  `Provider`, `StateHolder`, `Formatter`, …) instead of the generic `Service` suffix, and keep one
  class per class name to one responsibility. Do not mix raw data access (HTTP calls, JS interop)
  with state-holding/notification logic in the same class.
- Every Razor component gets a matching code-behind file (`Component.razor` + `Component.razor.cs`).
  Avoid `@code` blocks in `.razor` files.

---

# Implementation Principles

When implementing new features:

- Build the smallest useful solution.
- Do not over-engineer.
- Preserve architecture.
- Prefer readability over cleverness.
- Keep methods focused.
- Keep Razor components thin.
- Move business logic into Business layer.
- Keep Domain independent.
- Avoid unnecessary abstractions.
- Favor composition over inheritance.

---

# Entity Framework

- Code First
- One migration per feature
- Configure entities using IEntityTypeConfiguration
- Avoid lazy loading
- Keep persistence concerns out of Domain
- Repositories belong in the `Data` project (`StudyHub.Data`), alongside `ApplicationDbContext` and
  migrations — not in `Infrastructure`. Reserve `Infrastructure` for actual external-infrastructure
  concerns (file storage, email, AI provider adapters, etc.).

---

# UI

Use Bootstrap whenever possible.

Pages should:

- contain presentation only
- call Business contracts
- avoid business logic

Backend API endpoints are considered part of the UI layer.

---

# AI Features

AI should help users learn instead of replacing learning.

Generated content should:

- preserve technical correctness
- explain concepts
- generate review questions
- optionally generate Anki flashcards

Never generate solutions that encourage academic dishonesty.

---

# Testing

Use xUnit.

Business logic should always be tested.

When changing:

- workflows
- mappings
- business rules
- validation

update or add tests accordingly.

---

# Validation

Before completing any implementation:

- Ensure the solution builds.
- Ensure all tests pass.
- Fix compiler warnings introduced by the change.
- Verify that architecture boundaries remain intact.

Typical commands:

```bash
dotnet build
dotnet test
```

---

# Git

- One feature per branch.
- Small focused commits.
- Clear commit messages.
- No unrelated refactorings.

---

# Implementation Reports

When completing a task, always provide:

## What changed

Summarize the implementation.

## Why

Explain the reasoning.

## Architecture

Explain which layers were affected.

## Validation

List executed build and test commands.

## Risks

Describe assumptions, limitations or future improvements.

---

# Long-Term Vision

StudyHub should evolve into a complete learning platform.

Planned milestones:

- Semester Management
- Course Management
- Dashboard
- Document Library
- Markdown Notes
- Study Sessions
- Calendar
- AI Summaries
- Flashcard Generation
- Anki Export
- Search
- Learning Analytics

Future versions may include:

- Handwritten note import
- OCR
- OneNote integration
- Chat with documents (RAG)
- Local LLM support
- Cloud synchronization