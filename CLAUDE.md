# CLAUDE.md

# Project

Name: StudyHub

StudyHub is a Blazor Web App designed to help computer science students organize their studies and improve learning efficiency.

The system is both:
- a daily productivity tool
- a portfolio project demonstrating modern .NET architecture

Core focus areas:
- Course management
- Study planning
- Document management
- AI-assisted learning
- Flashcard generation
- Statistics & analytics

---

# Tech Stack

- .NET 10
- Blazor Web App
- ASP.NET Core Identity
- Entity Framework Core
- SQL Server
- MudBlazor
- Clean Architecture principles
- MediatR (optional)
- FluentValidation
- Mapster

---

# Architecture Pattern

The project follows a **Composite Component Architecture Pattern** inspired by Clean Architecture.

## High-Level Structure

- Shared
- Data
- Logic
- UI
- Infrastructure
- Tests

Each feature is composed of these layers and assembled via contracts.

---

## Logic Layer

The Logic layer is divided into three sub-layers:

### Domain
- Core business models and rules
- Pure business logic
- No external dependencies

### Integration
- External systems and APIs
- Adapters and service clients

### Business
- Application use-cases
- Orchestration of domain + integration logic
- Coordinates workflows across features
- Must NOT contain infrastructure details

---

## Contracts-First Principle

All communication between layers is done via contracts (interfaces).

Rules:
- No direct instantiation of implementations in Business or UI layers
- Depend only on abstractions
- Implementations live in Infrastructure or Integration layer

Benefits:
- Testable architecture
- Loose coupling
- Replaceable services (AI, storage, APIs)

---

## UI Layer

- Presentation only (Blazor)
- No business logic
- Calls Business layer via contracts
- Razor components must remain thin
- Backend/API endpoints are part of UI layer

---

## Data Layer

- EF Core models and configurations
- Persistence only
- No business logic

---

## Infrastructure Layer

- External services (AI, email, storage, APIs)
- Implements contracts defined in Logic layer

---

# Coding Standards

- Follow SOLID principles
- Prefer composition over inheritance
- Keep methods small and focused
- Use dependency injection everywhere
- Prefer async/await
- Avoid static helper classes unless justified
- Optimize for readability over cleverness

---

# Entity Framework

- Code First approach
- One migration per feature
- Use IEntityTypeConfiguration for mapping
- Avoid lazy loading
- Prefer explicit relationships

---

# UI Guidelines

- Use MudBlazor components
- Keep Razor pages minimal
- Move logic into services or Business layer
- Ensure reusable components where possible

---

# Naming Conventions

- Classes: PascalCase
- Methods: PascalCase
- Private fields: _camelCase
- Interfaces: IName
- Async methods: suffix Async

---

# Testing Strategy

- Use xUnit
- Test business logic thoroughly
- Avoid UI testing unless necessary
- Prefer unit tests over integration tests for logic

---

# Git Workflow

- Small, focused commits
- One feature per branch
- Descriptive commit messages

---

# AI Behavior Rules

AI features must support learning, not replace it.

Generated content must:
- be technically correct
- include key concepts
- encourage understanding
- optionally generate flashcards and review questions

---

# Long-Term Vision (NOT implementation details)

- Study planner
- Course management system
- Document library
- Markdown-based notes
- AI summaries & tutoring
- Flashcard system (Anki export)
- PDF/chat-based learning assistant
- Learning analytics dashboard