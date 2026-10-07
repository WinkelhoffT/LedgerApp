# Feature Plan: Anki Integration (Dashboard "cards due" widget)

Status: Draft
Classification (per `CLAUDE.md`): **Large** — first integration with an external
service (comparable to "AI integration" in the Large list), plus a deployment/network
change (`docker-compose.yml`, see section 6) that `SCP-001` only allows on explicit
request. Implementation starts only after this plan is confirmed.

## 1. Goal

The Dashboard shows whether there are Anki cards to study today — new, learning,
and review cards — read live from the user's local Anki installation. When there is
something to study, the Dashboard says so prominently. When Anki is not reachable,
the Dashboard still loads and shows a neutral "Anki not connected" hint instead of
an error.

## 2. Scope

In scope:

- Reading today's due counts (new / learning / review) from Anki per top-level deck
  and in total.
- One new Dashboard card ("Anki – due today") with the states: loading, disabled,
  not reachable, nothing due, cards due.
- Configuration (enable/disable, AnkiConnect address, optional API key, timeout).
- Tests for the Business mapping, the Domain rule, the accessor's JSON handling, and
  the API endpoint.

Out of scope (later milestones, see `CLAUDE.md` "Long-Term Vision"):

- Creating/exporting cards to Anki (planned "Flashcard Generation" / "Anki Export";
  this plan's accessor is the natural base for it, but adds no write actions now).
- Opening Anki or starting a review session from StudyHub.
- Learning analytics/history (streaks, retention) based on Anki review logs.
- Mapping Anki decks to StudyHub Courses/Semesters.
- Caching or background polling.

## 3. Decisions

- **"Anki API" = AnkiConnect.** Anki has no public web API (AnkiWeb offers no API for
  third parties). The de-facto standard is the **AnkiConnect** add-on (add-on code
  `2055492159`), which exposes a JSON-RPC-style HTTP endpoint from the running Anki
  desktop app, default `http://127.0.0.1:8765`. Requests are `POST /` with
  `{ "action": "...", "version": 6, "params": { ... }, "key": "<optional apiKey>" }`
  and responses are `{ "result": ..., "error": null | "<message>" }`.
  Consequence: counts are only available **while Anki desktop is running** with the
  add-on installed. This is an accepted limitation, handled by the "not reachable"
  UI state.
- **AnkiConnect actions used** (read-only):
  - `deckNames` → list of all deck names.
  - `getDeckStats` with `{ "decks": [<top-level deck names>] }` → per deck
    `new_count`, `learn_count`, `review_count`, `total_in_deck`. These are Anki's own
    "due today" numbers as shown in Anki's deck browser, i.e. they already respect the
    user's daily new/review limits — so StudyHub does not re-implement scheduling.
  - (`version` is not needed separately; a failed `deckNames` call already tells us
    Anki is unreachable.)
- **Only top-level decks are summed.** Anki's counts for a parent deck include its
  subdecks (`Informatik::Algorithmen` is part of `Informatik`), so summing every deck
  would double-count. "Top-level" = deck name without `::`. This is a domain rule and
  lives in the Domain layer (section 4).
- **"Has cards to study"** = `new + learning + review > 0` — also a domain rule, not
  inline in the orchestrator (per `CLAUDE.md` "Orchestrators … must not embed business
  rules", `NAM-002`).
- **No new NuGet dependency** (`DEP-001`): plain `HttpClient` + `System.Net.Http.Json`,
  same as the existing accessors.
- **Separate endpoint, separate load.** The Anki status gets its own endpoint
  (`GET api/dashboard/anki-status`) and is loaded independently in `Dashboard.razor.cs`,
  so a slow/unreachable Anki never delays the semester-progress hero card.
- **Short timeout, no exception to the UI.** AnkiConnect calls use a short timeout
  (default 3 s). Connection refused / timeout / AnkiConnect `error` are translated into
  an `Unavailable` status in the DTO, not into a 500 — Anki not running is a normal
  situation, not a server error.
- **No database impact.** Nothing is persisted; counts are read live on each Dashboard
  load. No migration.
- **No caching** (keeps `ADR01-001` "no in-memory shared-state caches" intact). Each
  Dashboard load does two small local HTTP calls — acceptable for a single-user tool.

## 4. Architecture Impact

Call chain (same shape as semester progress, plus one external hop):

```
Dashboard.razor(.cs)                         StudyHub.UI
  └─ IDashboardAccessor.GetAnkiStudyStatus   Logic.Integration  (HTTP → StudyHub.Api)
       └─ DashboardController                StudyHub.Api
            └─ IDashboardOrchestrator        Logic.Business
                 ├─ IAnkiConnectAccessor     Logic.Integration  (HTTP → AnkiConnect)
                 └─ IAnkiDueCardsCalculator  Logic.Domain(.Contract)
```

Business → Integration is the allowed direction (`CLAUDE.md` "Integration"); Integration
depends only on `Shared`.

| Layer | Project | Additions / changes |
|---|---|---|
| Shared | `StudyHub.Shared` | `Anki/AnkiDeckCountsDto.cs` (deck name, new, learn, review — what the AnkiConnect accessor returns), `Dashboard/AnkiStudyStatusDto.cs` (status, totals, per-deck list, `HasCardsToStudy`), `Dashboard/AnkiConnectionStatus.cs` (enum: `Disabled`, `Unavailable`, `Connected`), `Configuration/AnkiConnectOptions.cs` (`Enabled`, `BaseAddress`, `ApiKey?`, `TimeoutSeconds`), `Anki/AnkiConnectUnavailableException.cs` |
| Domain | `StudyHub.Logic.Domain.Contract` / `StudyHub.Logic.Domain` | `IAnkiDueCardsCalculator` + `AnkiDueCardsCalculator`: filters to top-level decks, sums counts, decides `HasCardsToStudy`. Returns a small result record (`AnkiDueCards`) |
| Business | `StudyHub.Logic.Business.Contract` / `StudyHub.Logic.Business` | `IDashboardOrchestrator.GetAnkiStudyStatusAsync()`; `DashboardOrchestrator` gets `IAnkiConnectAccessor`, `IAnkiDueCardsCalculator`, `IOptions<AnkiConnectOptions>` (6 deps total, within `COD-006`). Returns `Disabled` without calling Anki when the feature is off; catches `AnkiConnectUnavailableException` → `Unavailable`. DI registration in Business' `ServiceCollectionExtensions` |
| Integration | `StudyHub.Logic.Integration` | `Anki/IAnkiConnectAccessor.cs` + `Anki/AnkiConnectAccessor.cs` (`GetTopLevelDeckCountsAsync` or `GetDeckNamesAsync` + `GetDeckCountsAsync`), private request/response envelope records (one type per file, `COD-010`). Translates `HttpRequestException`, timeouts and non-null `error` into `AnkiConnectUnavailableException`. New `AddStudyHubAnkiConnect(IConfiguration)` extension (typed `HttpClient` with `BaseAddress`/`Timeout` from options), called from the **Api** composition root. `IDashboardAccessor`/`DashboardAccessor` get `GetAnkiStudyStatusAsync()` |
| UI (API) | `StudyHub.Api` | `DashboardController`: `[HttpGet("anki-status")]`. `Program.cs`: bind + validate `AnkiConnectOptions` on startup (`ValidateOnStart`, `COD-003`), call `AddStudyHubAnkiConnect`. `appsettings.json`: `AnkiConnect` section (no secrets) |
| UI (Blazor) | `StudyHub.UI` | `Components/Dashboard/AnkiStatusCard.razor` + `.razor.cs` (presentation only, receives `AnkiStudyStatusDto?` as parameter). `Dashboard.razor(.cs)`: load Anki status independently of semester progress, render the card |
| Data / Infrastructure | — | No changes |
| Tests | `StudyHub.Tests` | see section 7 |

Note on placement: `CLAUDE.md` lists "External Services / Adapters" under
**Integration**, while the comment in `StudyHub.Infrastructure/ServiceCollectionExtensions.cs`
reserves Infrastructure for "AI provider adapters" and similar. This plan follows
`CLAUDE.md` (Integration), because Business already references Integration and an
Infrastructure adapter would need a contract that Business can see anyway. Also,
`StudyHub.Logic.Integration.Contract` exists but is empty — existing accessor interfaces
live next to their implementations in `StudyHub.Logic.Integration/<Domain>/`; this plan
follows that existing convention instead of starting to use the empty contract project.
Flagging both per `CLAUDE.md` "report the inconsistency".

## 5. Contracts (sketch)

```csharp
// Shared/Dashboard
public enum AnkiConnectionStatus { Disabled, Unavailable, Connected }

public sealed record AnkiStudyStatusDto(
    AnkiConnectionStatus Status,
    bool HasCardsToStudy,
    int NewCount,
    int LearnCount,
    int ReviewCount,
    IReadOnlyList<AnkiDeckCountsDto> Decks);

// Shared/Anki
public sealed record AnkiDeckCountsDto(string DeckName, int NewCount, int LearnCount, int ReviewCount);

// Shared/Configuration
public sealed class AnkiConnectOptions
{
    public const string SectionName = "AnkiConnect";
    public bool Enabled { get; init; }
    public Uri BaseAddress { get; init; } = new("http://127.0.0.1:8765/");
    public string? ApiKey { get; init; }
    public int TimeoutSeconds { get; init; } = 3;
}

// Logic.Integration/Anki
public interface IAnkiConnectAccessor
{
    Task<IReadOnlyList<AnkiDeckCountsDto>> GetDeckCountsAsync(CancellationToken cancellationToken = default);
}

// Logic.Domain.Contract
public interface IAnkiDueCardsCalculator
{
    AnkiDueCards Calculate(IReadOnlyList<AnkiDeckCountsDto> deckCounts);
}

// Logic.Business.Contract
Task<AnkiStudyStatusDto> GetAnkiStudyStatusAsync(CancellationToken cancellationToken = default);
```

API: `GET api/dashboard/anki-status` → `200 AnkiStudyStatusDto` (also when Anki is
unreachable — then `Status = Unavailable`, counts 0).

## 6. Configuration & Deployment

`appsettings.json` (Api):

```json
"AnkiConnect": {
  "Enabled": true,
  "BaseAddress": "http://host.docker.internal:8765/",
  "TimeoutSeconds": 3
}
```

- `ApiKey` is **not** committed (`SEC-001`); if the user sets an API key in
  AnkiConnect's config, it is passed as env var `AnkiConnect__ApiKey`.
- **Docker networking (needs explicit approval, `SCP-001`):** the Api runs in a
  container, Anki runs on the host. `docker-compose.yml` needs for `studyhub-api`:
  ```yaml
  extra_hosts:
    - "host.docker.internal:host-gateway"
  ```
  (Docker Desktop on Windows/macOS resolves `host.docker.internal` already; Linux needs
  the `host-gateway` mapping.)
- **AnkiConnect config on the user's machine** (Anki → Tools → Add-ons → AnkiConnect →
  Config): by default AnkiConnect binds to `127.0.0.1` only. On Linux, container traffic
  arrives via the Docker bridge, so `webBindAddress` must be set to `0.0.0.0` (or the
  bridge IP). CORS (`webCorsOriginList`) is irrelevant because the call is server-side
  from the Api, not from the browser. This setup step gets documented in `README.md`.
- Local `dotnet run` without Docker: `appsettings.Development.json` overrides only
  `BaseAddress` to `http://127.0.0.1:8765/` (`DAT-005`).

## 7. Task Checklist

### Backend

- [ ] Shared: `AnkiConnectOptions`, `AnkiDeckCountsDto`, `AnkiStudyStatusDto`,
      `AnkiConnectionStatus`, `AnkiConnectUnavailableException`.
- [ ] Integration: `IAnkiConnectAccessor`/`AnkiConnectAccessor` (`deckNames` →
      `getDeckStats`), error translation, `AddStudyHubAnkiConnect` registration.
- [ ] Domain: `IAnkiDueCardsCalculator`/`AnkiDueCardsCalculator` (top-level filter,
      totals, `HasCardsToStudy`).
- [ ] Business: `DashboardOrchestrator.GetAnkiStudyStatusAsync` (Disabled /
      Unavailable / Connected mapping).
- [ ] Api: `DashboardController` endpoint, options binding + startup validation,
      `appsettings.json` section.
- [ ] Integration (UI side): `IDashboardAccessor.GetAnkiStudyStatusAsync`.

### UI

- [ ] `AnkiStatusCard` component (+ code-behind): "X cards due today" with
      new/learn/review breakdown and a top-decks list; highlighted when
      `HasCardsToStudy`; "All done for today" when nothing is due; muted hint
      "Anki not connected – start Anki with AnkiConnect" when `Unavailable`; hidden
      when `Disabled`.
- [ ] `Dashboard.razor(.cs)`: load the status independently; place the card in the
      dashboard grid (replacing nothing that is already real data).

### Deployment / Docs (after approval)

- [ ] `docker-compose.yml`: `extra_hosts` for `studyhub-api`.
- [ ] `README.md`: AnkiConnect setup (install add-on, `webBindAddress`, optional API
      key via env var).

### Tests (xUnit + Moq, existing stack)

- [ ] `AnkiDueCardsCalculatorTests`: subdecks not double-counted, totals, `HasCardsToStudy`
      true/false, empty deck list.
- [ ] `DashboardOrchestratorTests`: `Disabled` without calling the accessor;
      `Unavailable` on `AnkiConnectUnavailableException`; `Connected` mapping.
- [ ] `AnkiConnectAccessorTests` (fake `HttpMessageHandler`): request envelope
      (`action`, `version: 6`, `key`), parsing of `getDeckStats` result, non-null
      `error` → exception, connection failure/timeout → exception.
- [ ] `DashboardEndpointsTests`: `GET api/dashboard/anki-status` returns 200 with the
      orchestrator's DTO (accessor mocked via `WebApplicationFactory`).

## 8. Validation Plan

- `dotnet build` (no new warnings)
- `dotnet test`
- Manual: Dashboard with Anki closed (→ "not connected", semester card unaffected),
  Anki open with due cards (→ counts match Anki's deck browser), Anki open with nothing
  due (→ "All done"), `Enabled: false` (→ card hidden). Both via `dotnet run` and
  `docker compose up`.

## 9. Risks / Assumptions

- **Anki must be running.** AnkiConnect is only available while Anki desktop is open;
  the Dashboard cannot show counts otherwise. AnkiWeb sync data is not accessible
  without an official API.
- **Docker → host networking** differs per OS (Linux needs `host-gateway` +
  `webBindAddress: 0.0.0.0`). Binding AnkiConnect to `0.0.0.0` exposes it to the local
  network; recommend setting an AnkiConnect `apiKey` in that case (passed via env var).
- **Assumption:** "new cards to learn" means Anki's *due today* counts (new + learning
  + review, respecting daily limits), with "new" shown separately. If only *new* cards
  should trigger the highlight, `HasCardsToStudy` changes to `NewCount > 0` — a
  one-line domain-rule change.
- **AnkiConnect API stability:** `getDeckStats` field names (`new_count`,
  `learn_count`, `review_count`) are part of AnkiConnect API version 6; parsing is
  isolated in `AnkiConnectAccessor` so changes stay local.
- UI strings are hardcoded like the rest of the current Dashboard; `UIX-002`
  (localization) is not addressed here because no localization infrastructure exists yet.
