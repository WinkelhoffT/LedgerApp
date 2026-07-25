# Composite Components Architecture (.NET)

**Company Standard Architecture Documentation (based on David Tielke’s CoCo + company adaptations)**

## 0. Purpose, Scope, and Audience

This document defines our **standard software architecture** for .NET solutions based on **Composite Components (CoCo)** and our internal conventions (e.g., **no global DI/mappings project**, per-component DI registration, strict `internal` usage in implementations, etc.).

The architecture aims to create software that is:

- **Understandable** (fast navigation from domain to code structure)
- **Maintainable** (low change impact)
- **Testable**
- **Replaceable / extensible**
- **Not more complex than the domain**

CoCo treats software as a set of **components that interact via explicit contracts**, similar to industrial components that can be connected via standardized interfaces and replaced without changing the surrounding system.

---

## 1. Principles and Rationale (Non‑Negotiables)

### 1.1 EVA Principle: Everything is a “System” (Input → Processing → Output)

Everything in software can be understood as a **black box system**: it has defined **inputs**, internal **processing**, and defined **outputs**. This applies from applications down to statements.

**Architectural consequence:**  
A component must be usable as a black box. Its contract defines **input and output**, while its internal processing stays hidden.

**Principle evaluation:**

- **EVA:** maximized by treating components as black boxes.
- **Low coupling:** consumers depend only on defined I/O.
- **SRP:** forces one clear “processing responsibility” per component.

---

### 1.2 Keep Development Complexity from Growing Faster than Domain Complexity

As systems grow, uncontrolled complexity leads to more bugs, slower feature delivery, and eventually costly rewrites.

Tielke distinguishes:

- **Domain complexity (DK)**: concepts and business processes of the domain
- **Development complexity (EK)**: complexity visible in code and solution structure
- The implementation is a **mapping** from DK to EK

With **consistent structural rules**, EK can remain largely **constant**, even while DK grows.

**Architectural consequence:**  
We standardize structure (layers, components, contracts, DI patterns) so new features always “snap into” a known place.

**Principle evaluation:**

- **Transfer complexity:** EK must mirror DK, not inflate beyond it.
- **SRP + standard structure:** reduces the “mapping effort” (A) over time.

---

### 1.3 High Cohesion, Low Coupling

- **Coupling** is the amount of dependencies across modules; high coupling harms understanding, testability, and change safety.
- Good modularization targets **maximal cohesion** and **minimal coupling**.

**Architectural consequence:**  
Components are cut so that internal communication stays inside the component, and cross-component communication is explicit and minimized.

**Principle evaluation:**

- **High cohesion:** one task per component, logic stays together.
- **Low coupling:** dependencies only via contracts, not implementations.
- **Maintainability:** change impacts are localized.

---

### 1.4 SRP as the Main Tool for Modularization

Consistent application of SRP leads “automatically” to good modularization.  
For modularization, SRP means: only extract subsystems that represent **subtasks** of a single higher-level task (and each subsystem again has only one task).

**Architectural consequence:**  
We create a component when we identify a stable “one responsibility / one axis of change”.

**Principle evaluation:**

- **SRP:** explicit by design.
- **Transfer complexity:** responsibilities map to domain concepts.
- **Avoiding over-architecture:** if there is no concrete design issue, don’t introduce new patterns.

---

### 1.5 “Program to an Interface, not an Implementation” (DIP mindset)

Decoupling means dependencies should not be to concrete implementations, but to contracts/interfaces (“Program to an interface, not an implementation.”).

**Architectural consequence:**  
Every component is consumed through its **Contract**, never through its internal implementation types.

**Principle evaluation:**

- **Low coupling:** consumers remain stable while implementations evolve.
- **Replaceability:** components can be swapped without refactoring consumers.

---

## 2. Terminology

### 2.1 System, Layer, Component

- A **component** is a closed group of subtasks that jointly solves one task, is called via a defined interface, and calls other components via defined interfaces.
- A component has the typical properties: one task, offers an interface, consumes interfaces, reusable, exchangeable, testable.

### 2.2 Component Contract vs Component Implementation

A component is split into:

- **Contract**: all types that must be visible to users of the component
- **Implementation**: internal realization and private types

This split is fundamental to prevent “accidental coupling”.

### 2.3 Data Ownership

We distinguish between:

- **System-owned data**: data we store and control (our database / persistence)
- **Externally-owned data**: data fetched from external systems/APIs (integration)

This distinction drives where code belongs (**Data** vs **Logic/Integration**).

---

## 3. Architectural Overview

### 3.1 Architecture Definition (what we specify)

Software architecture defines the **inner structure** of a system (layers and components) without describing internal details of each component. It primarily targets quality attributes like reusability, replaceability, analyzability.

### 3.2 Our Macro Structure

We use:

- A **layered architecture** for orientation and dependency discipline
- **Composite Components** inside layers for modularization and decoupling

---

## 4. Layers (Company Standard)

> Naming uses numeric prefixes to keep ordering explicit in the repository/solution.

### 4.1 1_CrossCutting

Contains:

1. **Globally shared DataClasses** (only when strongly shared and not clearly owned by a single component)
2. **Shared technical utilities** (configuration fragments, logging/telemetry abstractions, shared helpers where unavoidable)

CoCo explicitly describes a separate **DataClasses project** in CrossCutting for types used by multiple layers/components; it is _not a component_, but a “global extension” of contracts that use these types.

**Rules**

- Prefer **local DTOs** in a component contract.
- Only promote to global DataClasses when truly cross-cutting and stable.

**DataClasses placement convention** (issue 086; see also section 9.3)

- **Component-local DTOs** live in `<Component>.Contract/DataClasses/` — used only within that
  component's own contract surface.
- **Global DTOs** (truly shared across multiple components/layers) live in
  `Polipol.PA.DataClasses` (CrossCutting).
- **Entities** (EF-tracked, ADR-02) live in `Polipol.PA.DataStoring.Contract/DataClasses/` and
  are reachable only from Domain Logic (`3_1`) — see ADR02-005 and section 8.1.

**Principle evaluation**

- **High cohesion:** prevents dumping everything into “common”.
- **Low coupling:** avoids turning CrossCutting into a hidden monolith.
- **Transfer complexity:** shared types should represent genuinely shared domain concepts.

---

### 4.2 2_Data

Contains **data persistence for system-owned data**:

- Repository contracts in `Polipol.PA.DataStoring.Contract`
- Repository implementations and EF configurations in `Polipol.PA.DataStoring`
- Entity types live in **`Polipol.PA.DataStoring.Contract.DataClasses`** (public; see ADR-02)
- `EntityTypeConfiguration` files (fluent API mapping) live in the implementation
- Data migrations (if applicable)

**Company rule (ADR-02):**

Entities are **domain types** that may cross between `2_Data.Contract` and **Domain Logic (3_1)** for mutation paths. They must NOT be consumed by Business Logic (3_3), Integration Logic (3_2), or UI (4) — those layers consume **DTOs** (returned by repository read methods or by Domain Management contracts).

EF mechanisms (change tracking, lazy loading, proxy types) stay internal — enforced by entity design:

- EF annotations (`[Timestamp]`, `[MaxLength]`, ...) live on `EntityTypeConfiguration` (fluent API), not on the entity class
- Postgres-specific types (`LTree`, etc.) used directly on entities are accepted as a deliberate trade-off (no abstraction overhead for theoretical ORM swap)

**Repository surface** (per aggregate):

- `Task<TDto?> GetById(Guid)` — read pipeline; `AsNoTracking()` projection at SQL level via `TEntity.ToDtoExpression`
- `Task<CappedResultDto<TDto>> GetFilteredAndCapped(filters, cap)` — materialized read; `Capper` consumed internally
- `Task<TEntity?> GetForMutation(Guid)` — tracked entity for mutation paths (omitted on immutable audit entities)
- `Task<TEntity> Add(CreateDto)` — returns the tracked entity; save is deferred to the outer `ExecuteInTransaction`

Repositories MUST NOT call `SaveChangesAsync`. The transaction boundary lives in the outer Domain Logic mutation method (see section 8.3 and ADR02-004).

**Principle evaluation**

- **SRP:** Data layer focuses on persistence and the technical projection of entities to DTOs; it does not encode domain workflows.
- **Low coupling:** other layers depend on stable DTOs (read paths) or on the public Entity contract (write paths); EF internals stay hidden.
- **EVA:** the repository surface is a true black box — no `IQueryable` escapes.

---

### 4.3 3_Infrastructure (Historical Note — Removed)

Historically the company standard included a `3_Infrastructure` legacy layer that hosted a central DI/mappings project (CoCo 1.0 sometimes calls this the “Bad Project”). It has been removed from the solution layout entirely; per-component `ServiceCollectionExtensions` (see section 7.3) replaces it.

This section is kept as a pointer for readers who encounter older repos, ADRs, or historical issue folders that still reference `3_Infrastructure`. Do not reintroduce the layer.

---

### 4.4 3_Logic

Logic is split into three sub-areas. The solution-folder order is **Domain → Integration → Business** (`3_1_DomainLogic`, `3_2_IntegrationLogic`, `3_3_BusinessLogic`). Integration appears before Business because Business typically consumes Integration; encountering Integration first in the folder tree reduces forward-reference cognitive load when reading the solution top-down. Going forward this is the standard ordering for all logic sub-folders.

#### 4.4.1 Domain Logic

Purpose:

- Domain rules, domain operations, invariants
- Cross-aggregate orchestration and transaction boundaries (see section 8.3)
- High cohesion around a domain concept

This corresponds closely to **Domänenkomponenten**: they encapsulate meaningful operations and expose them via interfaces.

**Domain Logic owns the canonical mutation pattern (ADR-02):**

```csharp
// IUnitManagement (Domain Logic 3_1)
public Task<UnitDto> Archive(Guid id) =>
    unitOfWork.ExecuteInTransaction(async () =>
    {
        var unit = await unitRepository.GetForMutation(id)
            ?? throw new EntityNotFoundException($"Unit '{id}' does not exist.");
        // cross-aggregate validation here
        unit.SetArchived();
        return unit.ToDto();
    });
```

The Management/Service/Orchestrator wraps the work in `IUnitOfWork.ExecuteInTransaction` — that is the single save point. Repositories never save. Entity methods enforce entity-local invariants (see ADR-02 for the distributed-domain-enforcement model: schema constraints + entity methods + Management orchestration).

**Principle evaluation**

- **Transfer complexity:** domain rules live near the domain concepts (entity methods + Management orchestration).
- **Reusability:** domain logic is often reusable across workflows.
- **EVA:** Management is the gateway — nothing else mutates the aggregate.

---

#### 4.4.2 Integration Logic (External Systems)

Purpose:

- External service access (HTTP APIs, messaging, OAuth/token refresh, etc.)
- Mapping from external DTOs into internal models
- Reliability patterns (retry, circuit breaker) if needed (and only if justified)

This corresponds to **Integrationskomponenten**: outwardly it looks like calling a local component; internally it forwards to the external system.

**Company-specific placement rules (solution folders)**  
Integration logic is further organized under `3_Logic/3_2_IntegrationLogic`:

- `3_Logic/3_2_IntegrationLogic/3_2_1_SAP` → integrations that call SAP APIs.
  - Component names should resemble the SAP endpoint called (e.g., `Polipol.PA.UnitInformation`).
- `3_Logic/3_2_IntegrationLogic/3_2_2_Backend` → integrations used by frontends to call backend APIs.
  - Components in this folder must use the suffix `Access` (e.g., `Polipol.PA.DemandBasedPlanningAccess`).
- `3_Logic/3_2_IntegrationLogic` (root) → other integrations not covered by SAP or backend API access.

**Principle evaluation**

- **EVA:** the integration component is a black box with clear input/output.
- **Low coupling:** consumers don’t know about remote APIs.
- **Transfer complexity:** prevents external concerns from polluting domain logic.

---

#### 4.4.3 Business Logic (Workflows / Orchestration)

Purpose:

- Orchestrates multiple domain components into a business process
- Represents “what the user triggers” and tends to change more frequently than domain rules

CoCo recommends implementing business processes as separate **Geschäftskomponenten / Workflow components**, to avoid unnecessary coupling between domain components.

**Principle evaluation**

- **Low coupling:** domain components remain independent.
- **SRP:** workflows are isolated to their own change axis.
- **Avoiding over-architecture:** we keep workflows simple unless the domain demands more.

---

### 4.5 4_UI

Contains entry points:

- Web APIs
- Web UIs
- Desktop apps
- Background services / jobs

In CoCo examples, UI entry points (e.g., a ConsoleClient) do not need a contract, because they are not reused as a component.

UI is also the natural place for the **application composition root** (wiring components).

**Principle evaluation**

- **SRP:** UI handles input/output, not domain rules.
- **EVA:** UI is the outermost “system boundary”.

---

## 5. Component Model (Composite Components)

### 5.1 What is a Component in Our Architecture?

A component is:

- One coherent responsibility (one task)
- Called through a stable contract
- Can call other components through their contracts

This mirrors the industrial concept of components: as long as the “contract” (interface) is honored, implementations can vary.

---

### 5.2 Component Categories (Mapping to Our Layers)

CoCo distinguishes categories of components:  
Foundation, Domain, Business, Integration.

We map them as follows:

| CoCo category | Our typical placement                                   | Notes                                          |
| ------------- | ------------------------------------------------------- | ---------------------------------------------- |
| Foundation    | 1_CrossCutting or 3_Logic (shared technical components) | logging, configuration, telemetry abstractions |
| Domain        | 3_Logic/Domain                                          | stable operations on domain concepts           |
| Integration   | 3_Logic/Integration                                     | external APIs, remote systems                  |
| Business      | 3_Logic/Business                                        | workflows/orchestration                        |

**Principle evaluation**

- **Transfer complexity:** keeps domain and workflow concerns separate.
- **Low coupling:** avoids “everything depends on everything”.

---

### 5.3 How to Slice Components

When we “cut” components, the key is to place each piece of functionality into the component that owns the responsibility and quality expectations (reusability, replaceability, testability).

**Anti-pattern to avoid:** coupling domain components by embedding workflows inside them. Instead, workflows belong in business components.

---

## 6. Contract vs Implementation

### 6.1 Why the Split Exists

The contract/implementation split is a core mechanism to:

- enforce **Dependency Inversion** at the component level
- keep coupling low
- allow independent evolution of implementations

The component implementation should depend only on:

- its own contract
- other component contracts

---

### 6.2 What Goes Into the Contract

A component contract contains everything consumers need to compile and use the component, typically:

- Interfaces (primary entry points)
- Data classes (DTOs) exchanged via the interface
- Delegates/callbacks
- Exceptions thrown across the boundary

**Folder convention (Contract project)**  
A typical contract has:

- root: interfaces
- `DataClasses/`
- `Delegates/`
- `Exceptions/` (often with a component-specific base exception type)

**Principle evaluation**

- **EVA:** contract defines I/O.
- **Low coupling:** only stable types cross boundaries.
- **SRP:** contract types describe usage, not internal mechanics.

---

### 6.3 What Goes Into the Implementation

Implementation should contain:

- All actual logic, algorithms, persistence logic, integration details
- Internal helper classes
- Internal adapters, mapping, validation, etc.

**Visibility rule (company standard):**

- All classes in the implementation are `internal` by default.
- Only explicit boundary classes may be `public` (e.g., DI registration helper).
- **Exception 1 (Options):** Options types required by a component may be `public` so the calling project can configure/bind them.
- **Exception 2 (Entities, ADR-02):** EF entities and their owned value objects in `Polipol.PA.DataStoring.Contract.DataClasses` are `public` so Domain Logic (3_1) can consume them for mutation paths via `IxxxRepository.GetForMutation`. Business Logic (3_3) and Integration Logic (3_2) must NOT consume entity types — see ADR02-005.

CoCo 2.0 explicitly recommends keeping implementation classes **internal** and exposing only a minimal creation mechanism to avoid unintended direct usage. The entity exception is a targeted relaxation of this rule for the specific case where the entity IS the domain object (rich domain methods + invariant guards) — see ADR-02.

**Principle evaluation**

- **Low coupling:** prevents “implementation reach-through”.
- **Replaceability:** implementation can be refactored without touching consumers.

---

## 7. Dependency Injection and Composition

### 7.1 The Problem with a Global Mappings/DI Project

Centralized mappings/DI tends to become a highly coupled “everything knows everything” project. CoCo literature calls out the risk of a special central project for DI/mappings (the “Bad Project”).

We removed the legacy `3_Infrastructure` layer entirely (see section 4.3) and rely on per-component `ServiceCollectionExtensions` instead.

---

### 7.2 CoCo 2.0 Approach: Component-local Activator

CoCo 2.0 introduces a component-local **Activator** that is the only visible type besides the contract and can create an instance fulfilling the contract, while internal classes remain hidden.

---

### 7.3 Company Adaptation: `ServiceCollectionExtensions` per Component

Instead of a custom Activator pattern, our .NET standard uses Microsoft DI:

Each component implementation contains a **public** `ServiceCollectionExtensions` class to register:

- the component’s contract → implementation mapping
- its internal collaborators
- required options/config bindings
- HttpClient registrations (integration)

**Rules**

- `ServiceCollectionExtensions` is the _only_ “composition-time API” of the implementation.
- The **UI layer** is the application’s overall composition root and calls each component’s `Add<...>()` method.

**Principle evaluation**

- **Low coupling:** no global mapping project.
- **SRP:** each component owns its DI configuration.
- **EVA:** composition is “input wiring”; execution still goes through contracts.

---

## 8. DataStoring vs IntegrationLogic (Data Ownership Rule)

### 8.1 System-Owned Data: DataStoring Components (Layer 2_Data)

Use **DataStoring** components when:

- we own the schema and persistence lifecycle
- we must ensure consistency, migrations, data rules

They expose repository-like contracts with two surfaces (ADR-02):

- **Read pipeline:** `GetById(Guid)`, `GetFilteredAndCapped(filters, cap)`, bespoke `Get*` methods — all return DTOs (projected at SQL level via `TEntity.ToDtoExpression`)
- **Mutation pipeline:** `GetForMutation(Guid)` returns the tracked Entity (consumed by Domain Logic 3_1); `Add(CreateDto)` returns the tracked Entity

The `Capper` is consumed only inside repositories. `IQueryable` does NOT cross the layer boundary.

**Rule:** entities are domain types accessible to Domain Logic (3_1) only — see ADR02-002, ADR02-005.

**Principle evaluation**

- **SRP:** persistence concerns remain isolated; the technical projection of entities to DTOs lives at the data boundary.
- **Low coupling:** Business Logic and UI depend on stable DTO contracts; only Domain Logic touches Entity types directly.
- **EVA:** repository surface is a true black box — no `IQueryable` escapes.

---

### 8.2 Externally-Owned Data: Integration Components (Layer 3_Logic/Integration)

Use **Integration** components when:

- data comes from external systems (CRM, ERP, identity provider)
- access involves remote protocols, tokens, retries, throttling
- we want local consumers to treat remote access like a local call

If external data must be stored internally (caching, history, reporting), then:

- **Integration** retrieves and maps external data
- **DataStoring** persists the internal representation

**Principle evaluation**

- **Transfer complexity:** separates external complexity from domain logic.
- **Low coupling:** keeps remote details contained.
- **EVA:** clear boundaries for I/O and processing.

---

### 8.3 Transaction Boundaries and Save Semantics

**Decision (ADR-02):** the transaction boundary is the **outer Domain Logic mutation method** (`*Management`/`*Orchestrator`/`*Service` in 3_Logic). It wraps work in `IUnitOfWork.ExecuteInTransaction(...)`. Repositories never call `SaveChangesAsync`.

**Why this matters:** prior to ADR-02 the codebase had a dual-mode `SaveChangesAsync` pattern — repositories committed individually outside an outer transaction, and flushed-only inside `ExecuteInTransaction`. This was undocumented, fragile, and the root cause of historical data-loss incidents (developers were unsure when changes would actually persist).

**Rules (ADR02-004):**

1. Repositories MUST NOT call `dataContext.SaveChangesAsync()`.
2. Every public mutation method in `*Management` / `*Orchestrator` / `*Service` (in 3_Logic) MUST wrap its work in `IUnitOfWork.ExecuteInTransaction(async () => { ... })`.
3. Read methods do NOT need a transaction wrap.
4. Cross-aggregate orchestrations rely on the **reentrant** `ExecuteInTransaction` model: nested calls participate in the outer transaction (verified by `Database.CurrentTransaction is not null` check).

**`IUnitOfWork.Flush(CancellationToken)`** exists for the rare case where a single logical operation needs to flush mid-transaction so subsequent reads see prior writes within the same transaction (used by `Polipol.PA.Rules.TransformationRuleExecutor.RunFor` between rule iterations). It does NOT commit. Use sparingly with an explanatory comment.

**`IUnitOfWork.Save(CancellationToken)`** is preserved for backwards compatibility but is not used in production code. It will be removed in a follow-up.

**Principle evaluation**

- **EVA:** the transaction boundary is exactly the public Domain method — no implicit dual-mode behavior.
- **Low coupling:** Repositories own persistence primitives; Domain Logic owns "when does this commit".
- **Reliability:** forgotten saves cause loud test failures (no state persists), not silent data loss.

---

## 9. Solution and Project Structure

### 9.1 Solution Folders by Layer

CoCo describes creating solution folders for layers such as UI, Logic, Data, CrossCutting.

We use:

- `1_CrossCutting`
- `2_Data`
- `3_Logic`
- `4_UI`
- `5_Tests`

(The historical `3_Infrastructure` layer has been removed — see section 4.3.)

Integration logic solution sub-folders:

- `3_Logic/3_2_IntegrationLogic/3_2_1_SAP`
- `3_Logic/3_2_IntegrationLogic/3_2_2_Backend`
- `3_Logic/3_2_IntegrationLogic` (root for other integrations)

---

### 9.2 Two Projects per Component

Each component consists of **two .NET projects**:

- `<Component>.Contract`
- `<Component>.Implementation` (or simply `<Component>` if your naming convention omits the suffix)

**Reference rules**

- Any consumer references only `<Component>.Contract` for compile-time use.
- Only composition roots reference `<Component>` (the implementation) to call `ServiceCollectionExtensions`.

**Principle evaluation**

- **Low coupling:** prevents “implementation dependency creep”.
- **SRP:** contract is the usage API; implementation is private.

---

### 9.3 Global DataClasses Project

A global `DataClasses` project in CrossCutting can hold domain DTOs shared across multiple layers/components. It’s not a component; it’s a shared type library acting as a global extension of contracts that use those types.

**Rule of thumb**

- Local DTO → keep in the component contract
- Global DTO → only if used across multiple components and no single owner exists

---

## 10. Visibility and Encapsulation Rules (`internal`)

### 10.1 Default: Implementation Types are `internal`

To prevent accidental coupling to implementation types:

- All implementation classes are `internal` by default.
- Only the minimum required boundary types are `public`.
- **Exception 1 (Options):** Options types required by a component may be `public` so the calling project can configure/bind them.
- **Exception 2 (Entities, ADR-02):** EF entities and their owned value objects in `Polipol.PA.DataStoring.Contract.DataClasses` are `public`. They may be consumed only by Domain Logic (3_1). See ADR02-003, ADR02-005.

CoCo 2.0 emphasizes keeping classes hidden so the system does not depend on implementation details. The entity exception preserves this principle for the layers that don't need entity access (Business, Integration, UI all consume DTOs).

### 10.2 Testing and Internals

Because implementations are internal:

- Tests may use `InternalsVisibleTo` to access internal types _only when necessary_.
- Prefer testing through the **contract behavior**, not internal structure.

**Principle evaluation**

- **Low coupling:** production code stays clean; tests get controlled access.
- **Testability:** preserved as a key component property.

---

## 11. Testing Strategy (Company Standard)

### 11.1 Test Projects per Component

For each component create separate test projects (suffix-based), for example:

- `<Component>.Tests`
- `<Component>.IntegrationTests`
- `<Component>.E2ETests`
- `<Component>.SystemTests` when integration and E2E coverage share mutable external infrastructure and must not run in parallel across assemblies

**Scope**

- Unit tests (.Tests): component logic in isolation, using mocks/stubs for other contracts
- Integration tests: verify integration boundaries (DB, external sandbox, testcontainers)
- End-to-end tests: belong to UI/application level
- System tests (.SystemTests): host the integration/E2E mix above in one non-parallel assembly when that shared infrastructure constraint is stronger than the project split

**Principle evaluation**

- **SRP:** tests reflect the responsibility boundaries.
- **Low coupling:** tests avoid spanning many components in a single suite.
- **EVA:** test inputs/outputs at component boundaries.

---

## 12. Dependency Rules and Allowed References

### 12.1 Golden Rule

> **No module depends on another module’s implementation — only on its contract.**

This is the practical meaning of “Program to an interface, not an implementation.”

### 12.2 Allowed Dependency Directions

Conceptually (simplified):

- `4_UI` → references contracts from `3_Logic`, `2_Data`, and (if needed) `1_CrossCutting`
- `3_Logic` → references contracts from `2_Data` and `1_CrossCutting`
- `2_Data` → references `1_CrossCutting`
- `1_CrossCutting` → references nothing (or only platform libraries)

---

## 13. Architectural Pitfalls and Anti‑Patterns

### 13.1 “Entourage” / Dependency Snowball

At component level, you can still accidentally create a monolith if components constantly depend on many others and changes ripple everywhere. CoCo warns that you can “still have a monolith” even with components if dependency discipline is ignored.

**Countermeasures**

- Enforce contract-only dependencies
- Introduce orchestration workflows instead of connecting domain components directly
- Keep CrossCutting minimal

---

### 13.2 CrossCutting as a Dumping Ground

Too much in CrossCutting increases global coupling.

- Prefer local DTOs
- Promote only truly global concepts

## 14. Reference Implementation Template

### 14.1 Solution Folder Layout Example

    src/
      1_CrossCutting/
        Company.Product.DataClasses/
      2_Data/
        Company.Product.CustomerData.Contract/
        Company.Product.CustomerData.Implementation/
      3_Logic/
        Company.Product.CustomerDomain.Contract/
        Company.Product.CustomerDomain.Implementation/
        Company.Product.CrmIntegration.Contract/
        Company.Product.CrmIntegration.Implementation/
        Company.Product.CustomerWorkflows.Contract/
        Company.Product.CustomerWorkflows.Implementation/
      4_UI/
        Company.Product.Api/
      5_Tests/
        Company.Product.CustomerDomain.Tests/
        Company.Product.CrmIntegration.IntegrationTests/

---

### 14.2 Minimal Contract Shape

    // CustomerDomain.Contract
    public interface ICustomerManager
    {
      Task<CustomerDto> GetById(CustomerId id, CancellationToken ct);
      Task<CustomerDto> Create(CustomerCreateDto dto, CancellationToken ct);
    }

---

### 14.3 Minimal Implementation + DI Registration

    // CustomerDomain.Implementation
    internal sealed class CustomerManager : ICustomerManager { /* ... */ }

    public static class ServiceCollectionExtensions
    {
      public static IServiceCollection AddCustomerDomain(this IServiceCollection services)
      {
        services.AddTransient<ICustomerManager, CustomerManager>();

        return services;
      }
    }

---

## 15. Review Checklist (Architecture Compliance)

Use this list in reviews:

### Components

- [ ] Does the component have exactly one responsibility?
- [ ] Are all cross-component calls done via contracts only?
- [ ] Is the contract minimal (only what consumers need)?

### Layering

- [ ] Does persistence live in `2_Data`, not in workflows or UI?
- [ ] Are external calls encapsulated in `3_Logic/Integration`?
- [ ] Are workflows separated from domain components?
- [ ] Are entity types (from `Polipol.PA.DataStoring.Contract.DataClasses`) consumed only by Domain Logic (3_1) — never by Business Logic, Integration Logic, or UI? (ADR02-005)

### Encapsulation

- [ ] Are implementation classes `internal` by default?
- [ ] Is `ServiceCollectionExtensions` the only intended public entry in the implementation?
- [ ] Are EF annotations on entity classes (e.g., `[Timestamp]`, `[MaxLength]`) moved to `EntityTypeConfiguration` — not on the entity itself? (ADR02-003)

### Persistence and Transactions

- [ ] Do mutation methods in `*Management`/`*Orchestrator`/`*Service` (3_Logic) wrap their work in `IUnitOfWork.ExecuteInTransaction(...)`? (ADR02-004)
- [ ] Do repository implementations avoid calling `SaveChangesAsync`? (ADR02-004)
- [ ] Do repositories return Entity from `GetForMutation` and DTO from `GetById`/`GetFilteredAndCapped`? (architecture 4.2)

### Shared Types

- [ ] Are DTOs local to a contract unless truly shared globally?

### Complexity Discipline

- [ ] Does the change fit into an existing standard structure (keeping EK stable)?
- [ ] Are we avoiding adding architecture complexity without a concrete design issue?

---

## 16. Summary (What This Architecture Optimizes For)

- **Stable component contracts** + **hidden implementations** → low coupling and safe refactoring
- **Workflow/business components** → protect domain components from process churn
- **Integration components** → external systems become local-looking black boxes
- **Consistent rules** → development complexity stays controlled over time

---

## 17. Deliberate Non-Adoptions (vs CoCo 2.0)

CoCo 2.0 (see `docs/external/coco-2.0-architecture-rules.md`, a source document, not an authority
— repo docs win on conflict) recommends several mechanisms we deliberately did not adopt
as-is. Each deviation is intentional and citable:

- **Rich entities over anemic POCO entities (ADR-02).** CoCo 2.0's default keeps EF entities
  fully internal and anemic. We relax this specifically for Domain Logic (`3_1`): entities carry
  behavior (state-transition methods, invariant guards) and are exposed to Domain Logic only —
  see ADR-02, ADR02-002, ADR02-005.
- **`ServiceCollectionExtensions` over a component-local Activator (section 7.3).** CoCo 2.0's
  component-local Activator pattern is replaced by Microsoft DI's per-component
  `ServiceCollectionExtensions`, wired from the UI composition root.
- **Global exceptions + problem-details over per-component base exceptions.** Instead of a
  per-component exception hierarchy (CoCo 2.0's contract `Exceptions/` convention with a
  component-specific base type), we use a small set of global exception types plus centralized
  ASP.NET Core problem-details mapping (`CustomizeProblemDetails`) — see API-004.
- **Assembly `ArchitectureLayer` stamp over layer-in-namespace.** Layer membership is declared via
  `[assembly: AssemblyMetadata("ArchitectureLayer", "...")]` rather than encoded into namespaces.
  This avoids a large one-time namespace-rename churn (a non-adopted alternative, "L05", from the
  issue 086 discovery) while still making layer membership machine-checkable — see section 18.
- **Targeted framework containment over a global framework abstraction / `IEventBroker`.**
  Rather than introducing a generic messaging/event abstraction to hide all frameworks uniformly,
  we contain each framework (EF Core/Npgsql, Telerik, NATS) to its owning component(s) directly
  (rules F01–F03). A global abstraction was judged unjustified complexity per the "don't introduce
  new patterns without a concrete design issue" principle (section 1.4).

---

## 18. Architecture Fitness Enforcement

Every `Polipol.PA.*` production assembly carries an `ArchitectureLayer` stamp:

```xml
<ItemGroup>
  <AssemblyMetadata Include="ArchitectureLayer" Value="DomainLogic" />
</ItemGroup>
```

Values: `CrossCutting`, `Data`, `DomainLogic`, `IntegrationLogic` (root, `.Sap`, `.Backend`),
`BusinessLogic`, `UI`.

## 19. Oversized C# Files

Most hand-written .cs files should stay at approximately 250 lines or fewer. Files that grow beyond this guideline should trigger a review of responsibilities and cohesion and, where appropriate, be split into smaller focused types or collaborators.
