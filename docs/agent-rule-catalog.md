# Agent Rule Catalog

This file contains the canonical, ID-based rule set for coding agents.

* Canonical source: `AGENTS.md`
* Project: Aplauncher
* Rules marked as `Auto` should be enforced by analyzers, architecture tests, linters, or CI.
* Rules marked as `Manual` must be checked by the implementing agent and reviewer.
* Rules marked as `Auto + Manual` require both automated checks and contextual review.

## Governance

| Rule ID | Rule                                                                                                                                                                                            | Check type | Source      |
| ------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------- | ----------- |
| GOV-003 | Stop and ask for clarification when ambiguity affects correctness, security, data integrity, or public contracts. For non-critical ambiguity, proceed minimally and state the assumptions made. | Manual     | `AGENTS.md` |
| GOV-005 | Work in small increments and avoid unrelated changes. Do not expand the scope of a task without a concrete reason.                                                                              | Manual     | `AGENTS.md` |
| GOV-006 | Final summaries must explain what changed and why it changed.                                                                                                                                   | Manual     | `AGENTS.md` |
| GOV-007 | When a build, test, lint, or startup check fails, attempt to identify and fix the cause. If unresolved, report what failed, what was attempted, and what human input is required.               | Manual     | `AGENTS.md` |
| GOV-008 | Follow the worktree, branch, commit, and push conventions defined by the `/issue-workflow:orchestrator` workflow.                                                                               | Manual     | `AGENTS.md` |

## Scope and security

| Rule ID | Rule                                                                                                                                                                                                       | Check type    | Source      |
| ------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------- | ----------- |
| SCP-001 | The default editable scope is `./src`, `./contracts`, `./tests`, `./docs`, and application settings files. Do not modify CI, build, deployment, or environment infrastructure without an explicit request. | Manual        | `AGENTS.md` |
| SEC-001 | Never commit secrets, access tokens, passwords, certificates, connection credentials, or private keys. Do not include them in source code, configuration, logs, tests, or documentation.                   | Auto + Manual | `AGENTS.md` |

## Architecture

| Rule ID | Rule                                                                                                                                                                                                                                   | Check type    | Source      |
| ------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------- | ----------- |
| ARC-001 | Treat `docs/architecture.md` and relevant ADRs as required input for implementation and review.                                                                                                                                        | Manual        | `AGENTS.md` |
| ARC-002 | Components should be separated into Contract and Implementation projects. Compile-time consumers reference contracts rather than foreign implementations.                                                                              | Auto + Manual | `AGENTS.md` |
| ARC-003 | Enforce inward-facing layer dependencies. UI is the outermost layer, Logic must not depend on UI, Data must not depend on Logic or UI, and CrossCutting must not depend on business layers.                                            | Auto          | `AGENTS.md` |
| ARC-005 | Classify logic as Domain, Business, or Integration logic. Keep workflow coordination and cross-component orchestration out of Domain components.                                                                                       | Manual        | `AGENTS.md` |
| ARC-007 | Controllers and Business or Workflow components must not call aggregate repositories directly. Repository access belongs behind the appropriate Domain or Application boundary.                                                        | Auto + Manual | `AGENTS.md` |
| ARC-008 | Blazor pages must pair a `.razor` file with a same-name `.razor.cs` code-behind file.                                                                                                                                                  | Auto          | `AGENTS.md` |
| ARC-010 | JetStream streams, consumers, and key-value resources are managed declaratively. Application code must not create or update infrastructure resources at runtime.                                                                       | Manual        | `AGENTS.md` |
| ARC-013 | Keep DTOs local to their component contracts unless they are genuinely cross-cutting, stable, and shared by multiple components. Keep CrossCutting minimal.                                                                            | Manual        | `AGENTS.md` |
| ARC-015 | Each component exposes its dependency-registration API through a `ServiceCollectionFactory`. Components are composed only at the application composition root. UI projects should remain boundary-focused and contain no domain rules. | Auto + Manual | `AGENTS.md` |

### Open architecture decision

`ARC-006` is intentionally not active yet. The project still needs a documented decision for integration folder placement, naming, and boundaries.

## Persistence and transactions

| Rule ID   | Rule                                                                                                                                                                                                                            | Check type    | Source      |
| --------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------- | ----------- |
| ADR02-001 | Persistence concerns belong to the Data layer. Transaction boundaries are defined by the outer mutation operation rather than being hidden inside individual repositories.                                                      | Manual        | `AGENTS.md` |
| ADR02-002 | Persistence entities may cross only between the Data layer and Domain Logic for mutation paths. Business Logic, Integration Logic, and UI consume DTOs or contract models. Persistence-framework behavior must remain internal. | Auto + Manual | `AGENTS.md` |

## API

| Rule ID | Rule                                                                                                                                                                           | Check type    | Source      |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------- | ----------- |
| API-003 | Follow the project controller and route naming conventions. Controllers use singular names, routes use plural kebab-case names, and endpoints return purpose-appropriate DTOs. | Auto + Manual | `AGENTS.md` |
| API-004 | Controllers must not perform ad hoc exception-to-response conversion. Exceptions bubble to the centralized exception mapping or problem-details mapping.                       | Manual        | `AGENTS.md` |
| API-005 | Minimize API input surfaces. Use purpose-built request DTOs containing only the fields required for the operation.                                                             | Manual        | `AGENTS.md` |

## Data access and configuration

| Rule ID | Rule                                                                                                                                                         | Check type    | Source      |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------- | ----------- |
| DAT-001 | Prefer navigation-property traversal over equivalent manual joins when relationships are already modeled.                                                    | Manual        | `AGENTS.md` |
| DAT-002 | Treat method inputs as strict contracts. Do not silently trim, normalize, replace, or repair invalid values unless normalization is an explicit requirement. | Manual        | `AGENTS.md` |
| DAT-005 | Environment-specific `appsettings.*.json` files contain only values that differ from the base `appsettings.json`.                                            | Auto + Manual | `AGENTS.md` |
| DAT-006 | For schema changes, create a new migration. Do not edit an existing migration that may already have been applied.                                            | Manual        | `AGENTS.md` |

## Coding conventions

| Rule ID | Rule                                                                                                                                                                                                                                                                                         | Check type    | Source      |
| ------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------- | ----------- |
| COD-001 | Do not add the `Async` suffix automatically. Use it only when required to distinguish synchronous and asynchronous variants.                                                                                                                                                                 | Auto + Manual | `AGENTS.md` |
| COD-002 | Signal collection mutability in public contracts. Use read-only collection types such as `IReadOnlyList<T>` when callers must not modify the collection.                                                                                                                                     | Manual        | `AGENTS.md` |
| COD-003 | Configuration classes use the `Options` suffix. Required configuration is validated during application startup.                                                                                                                                                                              | Manual        | `AGENTS.md` |
| COD-005 | Respect the project guardrails for class and method size. Refactor code that exceeds mandatory size or complexity thresholds.                                                                                                                                                                | Auto + Manual | `AGENTS.md` |
| COD-006 | Business-logic constructors must not exceed seven injected dependencies. More dependencies require decomposition or an explicit architectural justification.                                                                                                                                 | Auto + Manual | `AGENTS.md` |
| COD-007 | Do not add test-convenience defaults to production domain objects. Test defaults belong in builders, fixtures, factories, or test helpers.                                                                                                                                                   | Manual        | `AGENTS.md` |
| COD-008 | Follow the C# convention bundle: prefer pattern matching, switch expressions, file-scoped namespaces, `nameof`, nullable reference types, null-conditional access, consistent brace usage, and `Guid.CreateVersion7()` for new identifiers.                                                  | Auto + Manual | `AGENTS.md` |
| COD-010 | Each file contains exactly one declared top-level or nested type. Private and nested classes are not exempt and must be placed in their own files.                                                                                                                                           | Auto + Manual | `AGENTS.md` |
| COD-011 | Code comments may reference only durable and resolvable artifacts such as ADR IDs, rule IDs, architecture sections, issue folders, RFCs, or stable URLs. Do not reference temporary workflow context such as iterations, reviewer conversations, plan options, or the current merge request. | Manual        | `AGENTS.md` |
| COD-012 | Comments must explain non-obvious intent, rationale, trade-offs, or external constraints. Do not add comments that merely restate the code, describe the diff, narrate framework behavior, or justify a change to an imagined reviewer.                                                      | Manual        | `AGENTS.md` |

## Naming

| Rule ID | Rule                                                                                                                                                                                                                                                                                                                                                                                                                                          | Check type    | Source      |
| ------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------- | ----------- |
| NAM-002 | Orchestrator classes coordinate operations only. They must not contain embedded business rules.                                                                                                                                                                                                                                                                                                                                               | Manual        | `AGENTS.md` |
| NAM-003 | Class names must communicate their behavior through an approved suffix. The authoritative suffix list is defined by `NAM-005`.                                                                                                                                                                                                                                                                                                                | Auto + Manual | `AGENTS.md` |
| NAM-004 | DTO names use an approved role suffix such as `Dto`, `RequestDto`, `ResponseDto`, `CreateDto`, `Outcome`, `Change`, or `Result`.                                                                                                                                                                                                                                                                                                              | Auto + Manual | `AGENTS.md` |
| NAM-005 | Concrete classes use an approved behavioral suffix: `Reader`, `Management`, `Mapper`, `Parser`, `Repository`, `Accessor`, `Provider`, `Factory`, `Processor`, `Orchestrator`, `Formatter`, `Handler`, `Middleware`, `Worker`, `Controller`, `Extensions`, `Options`, `ViewModel`, or `Validator`. Generic bucket suffixes such as `Service`, `Helper`, and `Utility` are not allowed. Method names must match the behavior of the class type. | Auto + Manual | `AGENTS.md` |

## Layer boundaries

| Rule ID | Rule                                                                                                                                                                                                                                                      | Check type | Source      |
| ------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------- | ----------- |
| LAY-1   | CrossCutting assemblies may depend only on other CrossCutting assemblies.                                                                                                                                                                                 | Auto       | `AGENTS.md` |
| LAY-2   | Data assemblies may depend only on Data or CrossCutting assemblies.                                                                                                                                                                                       | Auto       | `AGENTS.md` |
| LAY-3   | UI is the outermost layer. No non-UI assembly may depend on a UI assembly.                                                                                                                                                                                | Auto       | `AGENTS.md` |
| LAY-4   | Apply a default-deny dependency matrix. A component may depend only on its own contract, explicitly allowed contracts from inner layers, and CrossCutting.                                                                                                | Auto       | `AGENTS.md` |
| LAY-5   | A component implementation may be referenced only by its own composition root, its own tests, or its own component. Other production components reference its contract.                                                                                   | Auto       | `AGENTS.md` |
| LAY-6   | An implementation assembly must not depend directly on another component's implementation assembly.                                                                                                                                                       | Auto       | `AGENTS.md` |
| LAY-7   | Domain Logic and Integration Logic must not depend directly on each other or on Business Logic. Coordination belongs in the appropriate outer workflow layer.                                                                                             | Auto       | `AGENTS.md` |
| LAY-8   | Persistence entity types may be referenced only by Data and Domain Logic. Other layers consume DTOs or contract models.                                                                                                                                   | Auto       | `AGENTS.md` |
| LAY-9   | Aggregate repositories may be consumed only by Data and Domain Logic. UI must never depend directly on repository interfaces. Dedicated read-model repositories may additionally be consumed by Business Logic when explicitly designed for that purpose. | Auto       | `AGENTS.md` |
| LAY-10  | Mutation-specific persistence APIs may be called only from Data or Domain Logic. Transaction APIs may be called only from Data or Logic layers.                                                                                                           | Auto       | `AGENTS.md` |
| LAY-11  | `IQueryable` must not appear on public contract surfaces or on public surfaces outside the Data layer.                                                                                                                                                    | Auto       | `AGENTS.md` |

## Contract and framework containment

| Rule ID | Rule                                                                                                                                                                                                                                                 | Check type | Source      |
| ------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------- | ----------- |
| CT01    | Types in `*.Contract` assemblies must be pure contract shapes such as interfaces, enums, delegates, records, exceptions, attributes, or DTOs. Contracts must not contain implementation behavior.                                                    | Auto       | `AGENTS.md` |
| F01     | EF Core and Npgsql types may only be referenced from `Aplauncher.DataStoring` and, where explicitly required, `Aplauncher.DataStoring.Contract`. They must not be referenced by API, UI, Business Logic, Integration Logic, or unrelated components. | Auto       | `AGENTS.md` |
| P02     | `DbContext`-derived types and `IEntityTypeConfiguration<>` implementations must live only in the `Aplauncher.DataStoring` namespace and project.                                                                                                     | Auto       | `AGENTS.md` |

## UI and ViewModels

| Rule ID | Rule                                                                                                                                                                     | Check type    | Source      |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------- | ----------- |
| UIX-002 | Do not hardcode user-visible UI strings. Store them in localization resources. Required project languages must contain a value or a deliberately documented placeholder. | Auto + Manual | `AGENTS.md` |
| UIX-004 | Follow the project ViewModel decision table for complex pages. Use ViewModels when a page has complex state, coordination, validation, or presentation logic.            | Manual        | `AGENTS.md` |

## Testing and validation

| Rule ID | Rule                                                                                                                                                                                                                                                                 | Check type    | Source      |
| ------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------- | ----------- |
| TST-001 | Follow the unit-test conventions for SUT naming, test naming, mocking, and assertions. Use FluentAssertions for .NET assertions. Do not add Arrange, Act, or Assert comments.                                                                                        | Auto + Manual | `AGENTS.md` |
| TST-002 | After code changes, run formatting, an analyzer-bearing build, the complete unfiltered .NET test suite, the JavaScript test suite when frontend code is affected, and all required startup checks. Do not hide failing tests through filters or exclusions.          | Auto + Manual | `AGENTS.md` |
| TST-003 | Verify every changed executable application by starting it with the project's test launch profile or documented test configuration.                                                                                                                                  | Manual        | `AGENTS.md` |
| TST-007 | New .NET test projects use the project's established test framework and FluentAssertions version 6.1.0. Frontend or JavaScript test projects use the repository's established JunitXml.TestLogger. Do not introduce a second assertion library or test runner without approval. | Auto + Manual | `AGENTS.md` |
| TST-012 | Test filenames, class names, and method names describe observable behavior. Do not name tests after issue numbers, implementation phases, iterations, or review rounds.                                                                                              | Manual        | `AGENTS.md` |

## Dependencies

| Rule ID | Rule                                                                                                                      | Check type | Source      |
| ------- | ------------------------------------------------------------------------------------------------------------------------- | ---------- | ----------- |
| DEP-001 | Adding a new production dependency or upgrading an existing dependency to a new major version requires explicit approval. | Manual     | `AGENTS.md` |

## Runtime state

| Rule ID   | Rule                                                                                                                                                                                                                                              | Check type | Source      |
| --------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------- | ----------- |
| ADR01-001 | Application state must support horizontal scaling without split-brain or divergent state. Avoid unsafe global mutable state and in-memory shared-state caches. Distributed caches are allowed only when source-of-truth guarantees remain intact. | Manual     | `AGENTS.md` |

## Required agent workflow

Before making a change, the agent must:

1. Read `AGENTS.md`.
2. Identify the applicable rule IDs.
3. Read relevant architecture documents and ADRs.
4. Make the smallest sufficient change.
5. Run all applicable validation commands.
6. Report:

   * what changed,
   * why it changed,
   * which validations were run,
   * which validations failed or were blocked,
   * any assumptions or unresolved risks.

## Deferred decisions

The following topics are intentionally not part of the active catalog yet:

* Integration folder structure and naming conventions formerly covered by `ARC-006`
* Exact class and method size thresholds for `COD-005`
* Exact commands for the JavaScript test suite
* Exact application startup commands and launch profiles
* Exact namespaces allowed to reference persistence entities
* Whether read-model repositories may be consumed directly by Business Logic
