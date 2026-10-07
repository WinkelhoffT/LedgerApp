# Feature Plan: Anki "Due Today" on the Dashboard (Anki Python library)

Status: Proposed — waiting for review and explicit confirmation
Classification (per `CLAUDE.md`): **Large** — integration with an external system, a new
out-of-process runtime component (a small Python service), a new production dependency (`anki`
from PyPI, `DEP-001`) and deployment changes (`Dockerfile`, `docker-compose.yml`, `SCP-001`).
Implementation starts only after this plan is confirmed.

## 1. Goal

The Dashboard shows how many Anki cards are due **today** — new, learning and review cards, in
total and per deck — so the student sees at a glance whether there is something left to study.

The numbers come from the user's own Anki collection, read through Anki's official Python library
(the `anki` package on PyPI) — **without the AnkiConnect add-on and without Anki desktop having to
run**.

## 2. Background

- The first attempt ([WinkelhoffT/LedgerApp#22](https://github.com/WinkelhoffT/LedgerApp/pull/22),
  closed, not merged) read the counts through AnkiConnect. It was closed because AnkiConnect only
  answers while Anki desktop is running, and there is no always-on machine running Anki.
- "Internal Python API" means the `anki` package that Ankitects publish on PyPI. It is built from the
  same Rust core as Anki desktop (`anki.collection.Collection`, `col.sched.deck_due_tree()`) and
  reads the collection file (`collection.anki2`) directly. It needs no add-on and no running Anki.
- .NET cannot call that library in-process, so a small Python sidecar (the **Anki bridge**) offers
  exactly one read endpoint over HTTP, and only `StudyHub.Api` calls it.

### Spike results (verified locally with `anki==26.9.3`, Python 3.13)

| Question | Result |
| --- | --- |
| Can the live collection be opened while Anki desktop has it open? | **No.** Anki opens SQLite with `locking_mode=exclusive` and WAL. A second `Collection(path)` fails with `DBError: Anki already open, or media currently syncing.` |
| Can a copy be read while Anki has the collection open? | **Yes.** If you copy `collection.anki2` together with `collection.anki2-wal` into a temp directory and open the copy, it works, and the copy includes changes that so far exist only in the WAL. Once Anki closes, only `collection.anki2` is left, because Anki switches the journal mode back to `delete`. |
| Which numbers does `deck_due_tree()` return? | The numbers Anki's deck browser shows (`new_count`, `learn_count`, `review_count`), **with the daily limits applied**: 25 new cards in a subdeck count as 20. The root node holds the totals, which equal the sum of the top-level decks. A node's `name` is only the leaf name (`Algorithmen`, not `Informatik::Algorithmen`), and `level` gives its depth. |
| What counts as "today"? | Anki's day, which ends at the rollover hour (default 04:00) **in the process's local time zone**. With `TZ=UTC` the cutoff is 04:00 UTC; with `TZ=Europe/Berlin` it is 02:00 UTC. Opening a collection also writes `localOffset` into it. That is harmless on a copy and must never happen to the original. |

## 3. Scope

In scope:

- Anki bridge (Python): `GET /health` and `GET /due-today`, which read a snapshot copy of one
  configured collection.
- .NET side: an accessor in `Logic.Integration`, a Domain rule, an orchestrator method and
  `GET api/dashboard/anki-status`.
- A Dashboard card "Anki – due today" with these states: loading, disabled, bridge unavailable,
  collection not found, all done, cards due (plus an "as of" timestamp).
- Docker: the bridge as an additional target in the existing `Dockerfile` and a new `studyhub-anki`
  service in `docker-compose.yml`. README setup section.
- Tests: Python `unittest` for the bridge, xUnit + Moq for .NET.

Out of scope (later, see section 11):

- Any write access to Anki: adding cards, studying, syncing. The original collection is mounted
  **read-only**.
- Syncing with AnkiWeb (see 4.2) and a self-hosted sync-server mode.
- Pushing generated flashcards into Anki (export stays CSV, see `flashcards-plan.md`).
- Mapping Anki decks to Courses/Semesters, review history/streaks, analytics, forecasts for future
  days.
- Caching, background polling, persistence.

## 4. Decisions

### 4.1 Data source: a snapshot of the local Anki profile

- The bridge reads the `collection.anki2` of **one** Anki desktop profile:
  - Windows: `%APPDATA%\Anki2\<Profile>\`
  - macOS: `~/Library/Application Support/Anki2/<Profile>/`
  - Linux: `~/.local/share/Anki2/<Profile>/`
- The profile folder is mounted **read-only**. The bridge never opens the original with
  `Collection(...)`: that would collide with Anki's exclusive lock, and the library writes to every
  collection it opens.
- On each request the bridge makes a consistent snapshot copy into a fresh temp directory, opens the
  copy, calls `deck_due_tree()`, closes the copy and deletes the temp directory.
- **Snapshot consistency:** the bridge records size and mtime of `collection.anki2` and `-wal`
  before and after copying both. If anything changed, it retries (at most 3 attempts with a short
  pause). If it still fails, the result is `collection_unreadable`. If `collection.anki2` is
  missing, the result is `collection_not_found`.
- This works whether Anki desktop is open or closed.

### 4.2 Why not AnkiWeb sync, and why not AnkiConnect

- **AnkiConnect** needs a running Anki desktop, which is why
  [WinkelhoffT/LedgerApp#22](https://github.com/WinkelhoffT/LedgerApp/pull/22) was closed.
- **AnkiWeb sync through the library:** the API exists (`col.sync_login`, `col.sync_collection`),
  but it would make the bridge a third-party sync client. AnkiWeb's terms only allow sync access
  through the approved clients (Anki, AnkiMobile, AnkiDroid, AnkiUniversal), so this option is
  ruled out.
- The officially supported alternative is a **self-hosted sync server**, which ships in the same
  `anki` package (`python -m anki.syncserver`). It stays a follow-up (section 11) because it would
  mean moving all of the user's Anki devices off AnkiWeb.

### 4.3 How .NET talks to Python: an HTTP sidecar

- The sidecar uses only the Python standard library (`http.server.ThreadingHTTPServer`, `json`,
  `shutil`, `tempfile`). The only third-party dependency is `anki` itself, pinned to an exact
  version.
- Rejected alternatives:
  - Spawning a Python process per request from the Api: this needs a Python runtime inside the .NET
    image, costs about 1 s of start-up per call, and reports errors through exit codes and stdout.
  - Embedding Python into .NET (pythonnet/CSnakes): this puts a native runtime into the Api
    process and brings GIL and deployment complexity.
- Only `StudyHub.Api` talks to the bridge, over the internal Compose network (`expose` only, no host
  port, no authentication needed). From .NET's point of view the bridge is an external system, so
  its adapter lives in `Logic.Integration`, like the Claude adapter.

### 4.4 Counts and "today"

- The counts are exactly what Anki's deck browser shows, with limits applied. StudyHub does
  **not** re-implement any scheduling.
- Totals come from the root node of the tree, which is Anki's own sum. The bridge returns every deck
  with its full name and level. The Dashboard lists only top-level decks that have something due,
  because a parent deck's counts already include its subdecks.
- "Has cards to study" means new + learning + review > 0. This is a Domain rule
  (`IAnkiDueCardsCalculator`), as is the choice of which decks to show. Neither goes inline in the
  orchestrator (`NAM-002`).
- The bridge container runs with `TZ` set to the user's time zone (default `Europe/Berlin`) and with
  `tzdata` installed, so Anki's day boundary matches the desktop.

### 4.5 Freshness

- The counts reflect the local collection file. Reviews done on the phone show up once Anki desktop
  has synced, which it does by default when it opens and closes.
- The card shows "as of <time>", taken from the last modification of the collection (the newer
  mtime of `collection.anki2` and `-wal`), so stale numbers are recognizable.

### 4.6 Failure handling

- Bridge errors return HTTP 503 with `{ "error": "<code>", "detail": "…" }`:
  - `collection_not_found`
  - `collection_unreadable`: the snapshot stayed unstable, the file is corrupt, or the collection
    was written by a newer Anki version than the pinned library.
- The .NET accessor maps:
  - connection refused, timeout or any other 5xx → `AnkiBridgeUnavailableException`
  - `collection_not_found` → `AnkiCollectionNotFoundException`
- The orchestrator turns those exceptions into status values. Anki being unavailable is a normal
  state, not an HTTP 500 (same approach as #22).
- The Dashboard loads the Anki card independently of the semester progress, so a slow snapshot
  never delays the hero card.

### 4.7 No persistence, no caching

- Nothing is stored and there is no migration. Each Dashboard load takes one snapshot, which keeps
  `ADR01-001` intact.
- If copying turns out to be slow (large collection, or Docker Desktop bind mounts on
  Windows/macOS), a follow-up can add a bridge-side cache keyed by collection mtime/size and the
  current Anki day.

## 5. Architecture Impact

```
Dashboard.razor(.cs) → AnkiStatusCard                           StudyHub.UI
  └─ IDashboardAccessor.GetAnkiStudyStatusAsync                 Logic.Integration (HTTP → StudyHub.Api)
       └─ DashboardController   GET api/dashboard/anki-status   StudyHub.Api
            └─ IDashboardOrchestrator.GetAnkiStudyStatusAsync   Logic.Business
                 ├─ IAnkiDueCountsAccessor → AnkiBridgeAccessor Logic.Integration (HTTP → Anki bridge)
                 │    └─ GET /due-today                         src/AnkiBridge (Python) → snapshot of collection.anki2
                 └─ IAnkiDueCardsCalculator                     Logic.Domain(.Contract)
```

Business → Integration is the allowed direction. Integration depends only on `Shared` (LAY-7).

| Layer / project | Additions / changes |
| --- | --- |
| **Anki bridge** — `src/AnkiBridge/` (new, Python, outside `StudyHub.slnx`) | `requirements.txt` (`anki==26.9.3`); package `anki_bridge/` with `config.py` (env vars), `snapshot.py` (consistent copy incl. `-wal`), `due_counts.py` (open the snapshot, `deck_due_tree()`, flatten into full names; the tree mapping is a pure, separately testable function), `server.py` (`/health`, `/due-today`, JSON errors, one lock so snapshots never run in parallel), `__main__.py`; `tests/` (`unittest`) |
| Shared | `Anki/AnkiDeckDueCountsDto`, `Anki/AnkiCollectionDueCountsDto`, `Anki/AnkiBridgeUnavailableException`, `Anki/AnkiCollectionNotFoundException`, `Dashboard/AnkiStudyStatus` (enum), `Dashboard/AnkiStudyStatusDto`, `Configuration/AnkiBridgeOptions` |
| Domain | `Logic.Domain.Contract`: `IAnkiDueCardsCalculator`, `AnkiDueCards` (result record). `Logic.Domain`: `AnkiDueCardsCalculator` (`HasCardsToStudy`; decks to show = top level with due > 0, sorted by due count descending, then by name) |
| Business | `IDashboardOrchestrator.GetAnkiStudyStatusAsync`. `DashboardOrchestrator` gets `IAnkiDueCountsAccessor`, `IAnkiDueCardsCalculator` and `IOptions<AnkiBridgeOptions>` (6 dependencies, within `COD-006`). It returns `Disabled` without calling the bridge when the feature is off. DI registration of the calculator |
| Integration | `Anki/IAnkiDueCountsAccessor`, `Anki/AnkiBridgeAccessor` (typed `HttpClient`), internal wire records for the bridge JSON (one type per file, `COD-010`), `Anki/ServiceCollectionExtensions.AddStudyHubAnki(IConfiguration)` (options binding + `ValidateOnStart`, `COD-003`), called from the **Api** composition root only. `Dashboard/IDashboardAccessor` + `DashboardAccessor`: `GetAnkiStudyStatusAsync` |
| Api | `DashboardController`: `[HttpGet("anki-status")]`. `Program.cs`: `AddStudyHubAnki(...)`. `appsettings.json`: `AnkiBridge` section. `launchSettings.json`: local `BaseAddress` override, as done in #22 because `appsettings.Development.json` is gitignored |
| UI | `Components/Dashboard/AnkiStatusCard.razor` + `.razor.cs` (presentation only), CSS in `wwwroot/css/app.css`. `Dashboard.razor(.cs)`: load the Anki status independently and place the card below the hero card |
| Data / Infrastructure | No changes |
| Deployment | `Dockerfile`: new target `final-anki-bridge`. `docker-compose.yml`: new service `studyhub-anki` and `AnkiBridge__BaseAddress` for `studyhub-api` |

### Reuse from #22

The head commit of the closed PR (`6f63b79`) can still be fetched with
`git fetch origin pull/22/head`. These parts carry over with small adaptations:

- the Domain calculator (totals now come from the bridge; the top-level rule stays)
- the orchestrator method (new status values)
- `AnkiStatusCard` (new states and the "as of" line)
- the independent loading in `Dashboard.razor.cs`, and the CSS
- the `DashboardAccessor`/`DashboardController` extensions
- the matching tests

Everything specific to AnkiConnect is replaced rather than reused: `AnkiConnectAccessor` and its
envelope types, `AnkiConnectOptions`, `extra_hosts`, and the README section.

## 6. Contracts (sketch)

### Bridge HTTP API

```
GET /health      → 200 {"status":"ok"}

GET /due-today   → 200
{
  "collectionModifiedAt": "2026-10-07T07:41:12Z",
  "newCount": 22, "learnCount": 3, "reviewCount": 41,
  "decks": [
    { "deckId": 1712345678901, "name": "Informatik",             "level": 1, "newCount": 20, "learnCount": 3, "reviewCount": 30 },
    { "deckId": 1712345678902, "name": "Informatik::Algorithmen", "level": 2, "newCount": 20, "learnCount": 1, "reviewCount": 12 },
    { "deckId": 1712345678903, "name": "Mathe",                  "level": 1, "newCount": 2,  "learnCount": 0, "reviewCount": 11 }
  ]
}

                 → 503 {"error":"collection_not_found","detail":"…"}
                 → 503 {"error":"collection_unreadable","detail":"…"}
```

Bridge configuration (environment variables):

| Variable | Default | Purpose |
| --- | --- | --- |
| `ANKI_COLLECTION_PATH` | `/anki-profile/collection.anki2` | Collection inside the read-only mount |
| `ANKI_BRIDGE_HOST` | `0.0.0.0` | Bind address (`127.0.0.1` for local runs) |
| `ANKI_BRIDGE_PORT` | `8080` | Port |
| `TZ` | — (Compose: `Europe/Berlin`) | Defines Anki's "today" |

### .NET

```csharp
// Shared/Anki
public sealed record AnkiDeckDueCountsDto(string DeckName, int Level, int NewCount, int LearnCount, int ReviewCount);

public sealed record AnkiCollectionDueCountsDto(
    DateTimeOffset CollectionModifiedAt,
    int NewCount,
    int LearnCount,
    int ReviewCount,
    IReadOnlyList<AnkiDeckDueCountsDto> Decks);

// Shared/Dashboard
public enum AnkiStudyStatus { Disabled, Unavailable, CollectionNotFound, Available }

public sealed record AnkiStudyStatusDto(
    AnkiStudyStatus Status,
    bool HasCardsToStudy,
    int NewCount,
    int LearnCount,
    int ReviewCount,
    IReadOnlyList<AnkiDeckDueCountsDto> Decks,
    DateTimeOffset? CollectionModifiedAt);

// Shared/Configuration
public sealed class AnkiBridgeOptions
{
    public const string SectionName = "AnkiBridge";
    public bool Enabled { get; set; }
    public Uri? BaseAddress { get; set; }      // required when Enabled (validated on start)
    public int TimeoutSeconds { get; set; } = 15;
}

// Logic.Integration/Anki
public interface IAnkiDueCountsAccessor
{
    /// <exception cref="AnkiBridgeUnavailableException">Bridge unreachable, timed out, or collection unreadable.</exception>
    /// <exception cref="AnkiCollectionNotFoundException">No collection at the configured path.</exception>
    Task<AnkiCollectionDueCountsDto> GetDueTodayAsync(CancellationToken cancellationToken = default);
}

// Logic.Domain.Contract
public interface IAnkiDueCardsCalculator
{
    AnkiDueCards Calculate(AnkiCollectionDueCountsDto dueCounts);
}

public sealed record AnkiDueCards(bool HasCardsToStudy, IReadOnlyList<AnkiDeckDueCountsDto> DecksToShow);

// Logic.Business.Contract — IDashboardOrchestrator
Task<AnkiStudyStatusDto> GetAnkiStudyStatusAsync(CancellationToken cancellationToken = default);
```

Api: `GET api/dashboard/anki-status` always returns `200 AnkiStudyStatusDto`. When Anki is not
available, `Status` says why and the counts are 0.

## 7. Configuration and Deployment

`appsettings.json` (Api), with no secrets involved:

```json
"AnkiBridge": {
  "Enabled": true,
  "BaseAddress": "http://studyhub-anki:8080/",
  "TimeoutSeconds": 15
}
```

`Dockerfile`, new independent target (the existing targets stay unchanged):

```dockerfile
FROM python:3.12-slim-bookworm AS final-anki-bridge
# anki 26.9.3 ships manylinux_2_35 wheels (glibc >= 2.35); tzdata so TZ resolves Anki's day boundary.
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY src/AnkiBridge/requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY src/AnkiBridge/anki_bridge/ anki_bridge/
USER nobody
EXPOSE 8080
ENTRYPOINT ["python", "-m", "anki_bridge"]
```

`docker-compose.yml`:

```yaml
  studyhub-anki:
    build:
      context: .
      dockerfile: Dockerfile
      target: final-anki-bridge
    expose:
      - "8080"
    volumes:
      - ${ANKI_PROFILE_DIR:-./data/anki-profile}:/anki-profile:ro
    environment:
      - TZ=${TZ:-Europe/Berlin}
    healthcheck:
      test: ["CMD", "python", "-c", "import urllib.request; urllib.request.urlopen('http://localhost:8080/health')"]
      interval: 10s
      timeout: 3s
      retries: 5
    restart: unless-stopped
```

- `studyhub-api` gets no `depends_on` on the bridge. The Api must start without it, and an
  unreachable bridge simply shows up as `Unavailable`.
- `ANKI_PROFILE_DIR` goes into a local, uncommitted `.env` file, for example
  `ANKI_PROFILE_DIR=C:/Users/<you>/AppData/Roaming/Anki2/User 1`. If it is not set, an empty
  directory is mounted and the card shows "collection not found" with a setup hint.
- To run locally without Docker, the README documents:
  `python -m venv .venv`, `pip install -r requirements.txt`, then
  `ANKI_COLLECTION_PATH=… ANKI_BRIDGE_HOST=127.0.0.1 ANKI_BRIDGE_PORT=5260 python -m anki_bridge`.
  The Api's `launchSettings.json` sets `AnkiBridge__BaseAddress=http://localhost:5260/`.
- `.gitignore` gets `__pycache__/` and `.venv/`.

## 8. Task Checklist

### Anki bridge (Python)

1. Project skeleton `src/AnkiBridge/` with `requirements.txt` (`anki==26.9.3`); `.gitignore` entries.
2. `snapshot.py`: consistent copy of `collection.anki2` + `-wal` with the stat check and retry;
   `collection_not_found` / `collection_unreadable`.
3. `due_counts.py`: open the snapshot, `deck_due_tree()`, totals from the root node, flatten the
   tree into full names (`Parent::Child`) with `level`, `collectionModifiedAt`.
4. `server.py` + `__main__.py`: routes, JSON responses, error codes, logging, request lock.
5. `Dockerfile` target `final-anki-bridge`.

### Backend (.NET)

6. Shared: DTOs, enum, exceptions, `AnkiBridgeOptions`.
7. Integration: `IAnkiDueCountsAccessor`, `AnkiBridgeAccessor` (+ wire records), error mapping,
   `AddStudyHubAnki`.
8. Domain: `IAnkiDueCardsCalculator` / `AnkiDueCardsCalculator`, `AnkiDueCards`.
9. Business: `DashboardOrchestrator.GetAnkiStudyStatusAsync` + DI registration.
10. Api: `DashboardController` action, `Program.cs`, `appsettings.json`, `launchSettings.json`.
11. Integration (UI side): `IDashboardAccessor.GetAnkiStudyStatusAsync`.

### UI

12. `AnkiStatusCard`:
    - "N cards due today" with a new / learning / review breakdown and the top-level decks
    - highlighted when there is something to study, "All done for today" otherwise
    - "as of <time>"
    - muted hints for `Unavailable` ("Anki bridge not reachable") and `CollectionNotFound`
      ("Anki collection not found – check ANKI_PROFILE_DIR")
    - hidden when `Disabled`
13. `Dashboard.razor(.cs)`: load the card independently of the semester progress.

### Deployment / Docs (after approval, `SCP-001`)

14. `docker-compose.yml`: `studyhub-anki` service, `AnkiBridge__BaseAddress` for `studyhub-api`.
15. `README.md`: "Configure Anki" section:
    - profile path per OS and the `.env` file
    - local run without Docker
    - note on freshness (phone reviews after the desktop sync)
    - note on keeping the version pin at least at the desktop version
16. `docs/agent-context.md`: the bridge as the first out-of-process integration component, plus
    glossary entries.

## 9. Validation Plan

- `dotnet build StudyHub.slnx` with no new warnings; `dotnet test StudyHub.slnx` green.
- `python -m unittest discover -s src/AnkiBridge/tests -t src/AnkiBridge` green.
- **Python tests** (against the real `anki` library, using temp collections the tests create
  themselves; no user data):
  - `due_counts`:
    - totals from the root
    - full names and levels of subdecks
    - daily new-card limit respected (25 new in one deck → 20)
    - empty collection → zeros
  - `snapshot`:
    - copy works while another `Collection` has the original open (WAL included)
    - a change during copying triggers a retry and finally `collection_unreadable` (simulated)
    - missing file → `collection_not_found`
    - the temp directory is always removed
  - `server`: `/health`, `/due-today` 200 shape, 503 error codes (with a fake reader).
- **.NET tests** (xUnit + Moq, no real bridge):
  - `AnkiDueCardsCalculatorTests`:
    - `HasCardsToStudy` true/false
    - only top-level decks with due > 0
    - sort order
  - `DashboardOrchestratorTests`:
    - `Disabled` without calling the accessor
    - `Unavailable` / `CollectionNotFound` mapping
    - `Available` mapping incl. `CollectionModifiedAt`
  - `AnkiBridgeAccessorTests` (fake `HttpMessageHandler`):
    - JSON parsing
    - 503 + `collection_not_found` → `AnkiCollectionNotFoundException`
    - other 503 codes, connection failure and timeout → `AnkiBridgeUnavailableException`
  - `DashboardEndpointsTests`: `GET api/dashboard/anki-status` → 200 with the orchestrator's DTO.
- **Manual**, both with `dotnet run` + a local bridge and with `docker compose up`:
  - Anki closed → the counts match Anki's deck browser.
  - Anki open → counts still appear. Study a few cards on the desktop, reload the Dashboard → the
    counts drop.
  - `ANKI_PROFILE_DIR` not set → hint "collection not found". Bridge stopped → "not reachable".
    In both cases the semester card is unaffected.
  - Time zone: between midnight and 04:00 the counts still belong to the previous Anki day, as on
    the desktop.

## 10. Risks / Assumptions / Open Questions

### Open questions (please answer before implementation)

1. **Does StudyHub (Docker) run on the same computer as Anki desktop?** This plan needs the Anki
   profile folder to be mountable into the bridge container. If StudyHub should run somewhere else
   (for example on a NAS), the self-hosted sync-server variant from section 11 is the right
   approach instead.
2. **Which Anki desktop version and which profile** do you use? The library pin must be at least
   the desktop version, and the profile name goes into `ANKI_PROFILE_DIR`.
3. Is the time zone `Europe/Berlin` correct?
4. Is it acceptable that reviews done on the phone only show up after Anki desktop has synced?
5. **Approvals:**
   - new production dependency `anki` and a Python runtime in the repo (`DEP-001`)
   - changes to `Dockerfile` and `docker-compose.yml` (`SCP-001`)
6. **License:** `anki` is licensed under AGPL-3.0. The bridge imports it, so the bridge code should
   be published under an AGPL-compatible license if the repository is public (for example its own
   `src/AnkiBridge/LICENSE`). StudyHub's .NET code only talks to the bridge over HTTP. The
   repository currently has no LICENSE file, so this is the owner's decision.

### Risks and assumptions

- **Freshness:** the counts are only as current as the local collection (see 4.5). The "as of"
  timestamp makes this visible.
- **Version compatibility:** if Anki desktop is newer than the pinned library and has changed the
  collection schema, opening fails ("collection too new") → `Unavailable` (the bridge log names
  the cause). Mitigation: keep the pin at least at the desktop version and update both together.
- **Snapshot consistency:** a write during copying is detected by the stat check and retried. In
  the worst case one Dashboard load shows "Unavailable"; it never shows wrong numbers silently and
  never touches the original.
- **Performance:** every Dashboard load copies the collection (typically 10–100 MB; bind mounts on
  Docker Desktop for Windows/macOS are slower). Measured during implementation; caching is a
  follow-up (4.7).
- **Time zone:** without `TZ` the container would use UTC and shift Anki's day by 1–2 hours.
- **File permissions:** the bridge runs as `nobody`, and on Linux hosts the profile files must be
  readable for it (with the usual umask 022, SQLite creates them as 644; to be checked on the real
  host). Windows/macOS bind mounts are unaffected.
- **Assumption:** "the open cards for the respective day" means Anki's **due today** counts
  (new + learning + review, with daily limits), with new cards shown separately. A forecast for
  the following days is a follow-up.
- UI strings are hardcoded, like the rest of the current Dashboard. `UIX-002` (localization) is not
  addressed because no localization infrastructure exists yet.

### Documentation inconsistencies found while planning

These are reported per `CLAUDE.md`, not resolved by guessing:

1. `ARC-006` (integration folder placement) is still an open decision in
   `docs/agent-rule-catalog.md`. The bridge is the first out-of-process component; this plan
   places it at `src/AnkiBridge/` as a self-contained Python project with its tests next to it,
   outside `StudyHub.slnx`. Please confirm.
2. `COD-001` (no automatic `Async` suffix) contradicts the existing code, which uses `Async`
   throughout. This plan follows the existing code.
3. `TST-001`/`TST-007` require FluentAssertions, but `StudyHub.Tests` does not reference it and
   uses xUnit `Assert`. This plan follows the existing tests and adds no new test dependency.

## 11. Follow-ups (not part of this feature)

- **Self-hosted sync-server mode:** the bridge would no longer copy a local file. Instead it would
  keep its own copy of the collection and sync it (`sync_login`, `sync_collection`, download only)
  against a self-hosted `python -m anki.syncserver` that all Anki clients use. Then StudyHub can run
  on a different machine than Anki desktop, and phone reviews arrive without opening the desktop.
  The bridge's HTTP contract, and therefore everything on the .NET side, stays unchanged.
- A bridge-side cache keyed by collection mtime/size and the Anki day (see 4.7).
- A forecast for the next days (`prop:due=N` searches), and deck → Course mapping.
- Feeding the real Anki numbers into the placeholder stat tiles ("Flashcards reviewed this week")
  and into Learning Analytics.
