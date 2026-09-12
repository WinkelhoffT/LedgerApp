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

> The current implementation deviates from several rules below (Minimal APIs instead of
> Controllers, one fat API client per domain instead of Accessors, `@code` instead of Razor
> code-behind, repositories in `Infrastructure` instead of `Data`, an empty `Shared` project). See
> `review.md` for the full gap analysis and the target end-to-end workflow
> (Accessor → Controller → Orchestrator → Domain + Data). Treat the rules below as the target state
> for new and refactored code; migrating existing code is a "Large" change per the workflow
> classification above and needs a plan before implementation.

Projects:

- Shared
- Data
- Logic
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

- Entities
- Value Objects
- Domain Rules

No external dependencies.

---

## Business

Contains:

- Use Cases
- Application Workflows
- Orchestration
- Validation

Business coordinates Domain and Integration.

Business never accesses Infrastructure directly.

---

## Integration

Contains:

- API Clients
- AI Providers
- External Services
- Adapters

---

# Contracts First

All communication between layers happens through contracts.

Rules:

- Never instantiate implementations directly.
- Depend only on interfaces.
- Infrastructure implements contracts.
- UI communicates only with Business contracts.

Within each Logic component, keep the contract (interfaces, DTOs, exceptions) separated from its
implementation, e.g. a `Contracts/` subfolder or namespace per domain, rather than mixing DTO,
interface, exception, and implementation classes in the same folder.

Data classes, DTOs, and exceptions that are genuinely shared across multiple layers/components
belong in `Shared` (`StudyHub.Shared`), not in `Logic.Business`/`Logic.Domain`. Options/configuration
classes also belong in `Shared` (e.g. under a `Configuration` area). Keep component-local DTOs that
are only used within one domain in that domain's contract area instead of promoting everything to
`Shared`.

---

# API Layer

Use ASP.NET Core Controllers, not Minimal API endpoint groups, for `StudyHub.Api`. Controllers stay
thin (routing, model binding, status codes) and call an orchestrator from the Business layer —
never a repository or the Domain layer directly. Do not let a controller action embed workflow
logic; that belongs in the Business orchestrator it calls.

---

# UI Services and Frontend Integration

- The UI talks to `StudyHub.Api` through small, purpose-specific Accessor classes, not through a
  single class per domain that implements the full Business contract over HTTP. Avoid one "fat"
  API client per aggregate (e.g. one class implementing `ISemesterManagement` end-to-end); prefer
  narrower accessors that map to what a page actually needs.
- Name UI helper classes by role (`Accessor`, `Provider`, `StateHolder`, `Formatter`, …) instead of
  the generic `Service` suffix, and keep one class per class name to one responsibility. Do not mix
  raw data access (HTTP calls, JS interop) with state-holding/notification logic in the same class.
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