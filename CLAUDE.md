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
- `docs/roadmap.md`
- `docs/adrs/`

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

---

# UI

Use MudBlazor whenever possible.

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