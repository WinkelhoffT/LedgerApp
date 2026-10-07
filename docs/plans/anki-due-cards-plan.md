# Feature Plan: Anki "Due Today" on the Dashboard (Anki Python library + self-hosted sync server)

Status: Proposed (revision 2): waiting for the answers in section 10 before implementation
Classification (per `CLAUDE.md`): **Large** for four reasons:

- it integrates an external system;
- it adds new out-of-process runtime components (two small containers from Anki's Python package);
- it adds a new production dependency (`anki` from PyPI, `DEP-001`, approved);
- it changes deployment (`Dockerfile`, `docker-compose.yml`, `SCP-001`, approved).

Revision 2 targets the real deployment: StudyHub runs on a **Strato server**, and the user syncs
with **AnkiWeb** today. There is no Anki desktop collection on the server, so revision 1's
"mount the local Anki profile" no longer works. The counts now come from the user's **own Anki
sync server** on the same machine.

## 1. Goal

The Dashboard shows how many Anki cards are due **today**: new, learning and review cards, in
total and per deck. The student then sees at a glance whether there is something left to study.

The numbers come from the user's own Anki collection and are read with Anki's official Python
library (the `anki` package on PyPI). This works **without the AnkiConnect add-on** and **without
a running Anki desktop**, and it includes reviews done on the phone as soon as the phone has synced.

## 2. Background

- The first attempt ([WinkelhoffT/LedgerApp#22](https://github.com/WinkelhoffT/LedgerApp/pull/22),
  closed, not merged) used AnkiConnect. It was closed because AnkiConnect only answers while Anki
  desktop runs, and there is no machine where Anki desktop runs all the time.
- The "internal Python API" is the `anki` package that Ankitects publish on PyPI. It is built from
  the same Rust core as Anki desktop and offers `anki.collection.Collection` and
  `col.sched.deck_due_tree()`. It reads a collection file (`collection.anki2`) directly. The same
  package contains Anki's official **self-hosted sync server** (`python -m anki.syncserver`).
- **AnkiWeb is not an option as a data source.** The library could sync with AnkiWeb
  (`col.sync_login`, `col.sync_collection`), but AnkiWeb's terms only allow sync access from the
  approved clients (Anki, AnkiMobile, AnkiDroid, AnkiUniversal). A StudyHub service syncing with
  AnkiWeb would be an unapproved third-party client. AnkiWeb also has no read API.
- Consequence: the user's Anki apps sync with a **self-hosted sync server on the Strato server**
  instead of AnkiWeb. That server stores the latest state of the collection on disk. A small
  Python sidecar, the **Anki bridge**, reads a read-only snapshot of that file and gives
  `StudyHub.Api` the due counts over HTTP.

### Spike results (verified locally with `anki==26.9.3`, Python 3.13)

| Question | Result |
| --- | --- |
| Can a collection be opened while another process has it open? | **No.** Anki opens SQLite with `locking_mode=exclusive` and WAL. A second `Collection(path)` fails with `DBError: Anki already open, or media currently syncing.` This applies to Anki desktop and to the sync server, which keeps the collection open after a normal sync. |
| Can a copy be read while the file is open? | **Yes.** Copying `collection.anki2` together with `collection.anki2-wal` into a temp directory and opening the copy works. The copy also contains changes that so far exist only in the WAL. |
| Where does the sync server store the collection? | `SYNC_BASE/<username>/collection.anki2` (plus `-wal` while it is open, `media/`, `media.db`). |
| Does the stored server copy reflect client reviews right away? | **Yes.** A client uploaded a collection (`FULL_UPLOAD`, because the server was empty) and the server snapshot showed 22 new cards. The client then studied 2 cards and ran a normal sync. A new snapshot of the **running** server showed 20 new / 2 learning, identical to the client. |
| Which numbers does `deck_due_tree()` return? | The numbers Anki's deck browser shows, **with daily limits applied**: 25 new cards in a deck show as 20. The root node holds the totals, which equal the sum of the top-level decks. A node's `name` is only the leaf name (`Algorithmen`, not `Informatik::Algorithmen`), and `level` gives its depth. |
| What counts as "today"? | Anki's day, which ends at the rollover hour (default 04:00) **in the time zone of the process**: `TZ=UTC` puts the cutoff at 04:00 UTC, `TZ=Europe/Berlin` at 02:00 UTC. Opening a collection also writes `localOffset` into it. That is harmless on a copy, but it is why the original is never opened. |

## 3. Scope

In scope:

- **Anki sync server** (`anki-sync` service) on the Strato server, built from the same pinned
  `anki` package. The user's apps sync with it instead of AnkiWeb.
- **Anki bridge** (Python): `GET /health` and `GET /due-today`, reading a snapshot of the
  sync server's collection for one configured user.
- **.NET:** an accessor in `Logic.Integration`, a Domain rule, an orchestrator method and
  `GET api/dashboard/anki-status`.
- **Dashboard card "Anki – due today"** with these states: loading, disabled, bridge unavailable,
  collection not found (not synced yet), all done, cards due (plus an "as of" timestamp).
- **Docker:** one new `Dockerfile` target for both Anki services, two services in
  `docker-compose.yml`, `.gitignore` entries for `.env` and `data/`.
- **Docs:** README setup and a one-time migration guide from AnkiWeb (desktop and phone).
- **Tests:** Python `unittest` for the bridge, xUnit + Moq for .NET.

Out of scope (later, see section 11):

- Any write access to the collection from StudyHub. The bridge mounts the sync data
  **read-only**. StudyHub never studies, edits or syncs.
- An HTTPS reverse proxy or domain setup on the Strato server if none exists yet. It is a
  prerequisite (section 7.3); if it is missing, it becomes a separate, small deployment step.
- Pushing generated flashcards into Anki. Export stays CSV, see `flashcards-plan.md`.
- Mapping Anki decks to Courses/Semesters, review history and streaks, analytics, and forecasts for
  future days.
- Caching, background polling and persistence on the StudyHub side.

## 4. Decisions

### 4.1 Data source: the self-hosted sync server's collection

- `anki-sync` runs `python -m anki.syncserver` with one user (`SYNC_USER1=<name>:<password>`) and
  stores its data in `./data/anki-sync` on the server.
- Anki desktop and the phone app use this server instead of AnkiWeb. See 4.8 for the switch.
- The bridge mounts the same directory **read-only** and reads
  `/anki-sync/<name>/collection.anki2`. It needs **no credentials** and never talks to the sync
  server over the network.
- Both services run from the **same image** with the same pinned `anki` version, so the storage
  format the bridge reads always matches the one the server writes.

### 4.2 Snapshot reading

On each `/due-today` request, the bridge:

1. takes a consistent snapshot copy into a fresh temp directory,
2. opens the copy and runs `deck_due_tree()`,
3. closes the copy and deletes the temp directory.

It never opens the original: the sync server holds an exclusive lock on it, and the library writes
to every collection it opens.

**Consistency:** the bridge records size and mtime of `collection.anki2` and `-wal` before and after
copying both. If anything changed (a sync was running), it retries up to 3 times with a short pause.
If it still fails, it returns `collection_unreadable`. If the user has not synced yet and the file
is missing, it returns `collection_not_found`.

Locally (development) the bridge can point `ANKI_COLLECTION_PATH` at any collection, for example a
local Anki desktop profile or a test collection. The code path is the same.

### 4.3 .NET ↔ Python: an HTTP sidecar

- The bridge uses only the Python standard library (`http.server.ThreadingHTTPServer`, `json`,
  `shutil`, `tempfile`) plus the pinned `anki` package. There is no web framework.
- Rejected alternatives:
  - Spawning a Python process per request from the Api: this would need Python inside the .NET
    image and costs about 1 s of start-up per call.
  - Embedding Python into .NET (pythonnet/CSnakes): this brings a native runtime into the Api
    process and adds GIL and deployment complexity.
- Only `StudyHub.Api` talks to the bridge, over the internal Compose network (`expose` only, no
  host port). From .NET's point of view the bridge is an external system, so its adapter lives in
  `Logic.Integration`, like the Claude adapter.

### 4.4 Counts and "today"

- The counts are exactly what Anki's deck browser shows, with limits applied. StudyHub does
  **not** re-implement any scheduling.
- The totals are the root node of the tree, which is Anki's own sum. The bridge returns every deck
  with its full name and level. The Dashboard lists only top-level decks that have something due,
  because a parent's counts already include its subdecks.
- "Has cards to study" means new + learning + review > 0. This rule and the choice of which decks
  to show live in the Domain (`IAnkiDueCardsCalculator`), not inline in the orchestrator
  (`NAM-002`).
- The bridge container runs with `TZ=Europe/Berlin` (confirmed) and with `tzdata` installed, so
  Anki's day boundary (default 04:00) matches the apps.

### 4.5 Freshness

The counts reflect the last sync from **any** device (desktop or phone). Anki syncs automatically
when it opens and closes (desktop) or on demand (mobile).

The card shows "as of <time>", taken from the newer mtime of `collection.anki2` and `-wal`, so
outdated numbers are visible. Confirmed acceptable.

### 4.6 Failure handling

The bridge returns HTTP 503 with `{ "error": "<code>", "detail": "…" }` for two codes:

- `collection_not_found`
- `collection_unreadable`: the snapshot stayed unstable, the file is corrupt, or the format is
  newer than the library can read.

The .NET accessor maps these failures:

- connection refused, timeout, or any other 5xx → `AnkiBridgeUnavailableException`
- `collection_not_found` → `AnkiCollectionNotFoundException`

The orchestrator turns both into status values. Anki being unavailable is a normal state, not a
500.

The Dashboard loads the Anki card independently of the semester progress, so a slow snapshot never
delays the hero card.

### 4.7 No persistence and no caching in StudyHub

Nothing is stored and there is no migration. Each Dashboard load takes one snapshot, which keeps
`ADR01-001` intact.

A typical collection is 10–100 MB, so copying it on the server's local disk takes well under a
second. If that is too slow, a follow-up adds a bridge-side cache keyed by mtime/size and Anki day.

### 4.8 Switching from AnkiWeb to the own sync server (one-time, done by the user)

1. Deploy `anki-sync` (section 7) and make it reachable over HTTPS (section 7.3).
2. Anki desktop: sync with AnkiWeb one last time so desktop has the latest state.
3. Anki desktop: go to *Preferences → Syncing → Self-hosted sync server*, enter the URL, then sync
   and log in with the sync user. The server is empty, so Anki uploads the whole collection
   (the spike showed `FULL_UPLOAD`; media is synced too).
4. Phone:
   - AnkiDroid: *Settings → Sync → Custom sync server*. Sync URL `https://<host>/`, media URL
     `https://<host>/msync/`.
   - AnkiMobile (version 2.0.90 or newer): *Settings → Sync → Custom server*.
   - Log in, then choose **Download** for the full sync, so the server's state wins.
5. From then on, every device syncs with the own server. AnkiWeb keeps the old state as a backup
   but no longer receives updates.

Switching back is possible at any time: clear the custom URL, then do a full upload to AnkiWeb
from desktop.

### 4.9 Secrets

- The only secret in this feature is the **sync user's password**. The sync server container needs
  it, and so do the Anki apps when logging in. The .NET code and the bridge do not need it.
- It is **never committed**:
  - On the Strato server it goes into the uncommitted `.env` file next to `docker-compose.yml`
    (`ANKI_SYNC_USERNAME`, `ANKI_SYNC_PASSWORD`), in the same way the `ANTHROPIC_API_KEY` for
    Docker is handled.
  - `dotnet user-secrets` only applies to a local `dotnet run` (Development). In production on the
    server it is not available, so environment variables / `.env` are used there.
- `.gitignore` gets `.env` and `data/`. Right now neither is ignored, only `*.db*`. Without this,
  the synced collection, which is the user's personal learning data, and the password could end up
  in the **public** repository.

## 5. Architecture Impact

```
Dashboard.razor(.cs) → AnkiStatusCard                           StudyHub.UI
  └─ IDashboardAccessor.GetAnkiStudyStatusAsync                 Logic.Integration (HTTP → StudyHub.Api)
       └─ DashboardController   GET api/dashboard/anki-status   StudyHub.Api
            └─ IDashboardOrchestrator.GetAnkiStudyStatusAsync   Logic.Business
                 ├─ IAnkiDueCountsAccessor → AnkiBridgeAccessor Logic.Integration (HTTP → studyhub-anki)
                 │    └─ GET /due-today                         src/AnkiBridge (Python)
                 │         └─ snapshot of ./data/anki-sync/<user>/collection.anki2 (read-only)
                 │              ▲ written by anki-sync  ◄── HTTPS sync ── Anki desktop / AnkiDroid / AnkiMobile
                 └─ IAnkiDueCardsCalculator                     Logic.Domain(.Contract)
```

Business → Integration is the allowed direction. Integration depends only on `Shared` (LAY-7).

| Layer / project | Additions / changes |
| --- | --- |
| **Anki bridge**: `src/AnkiBridge/` (new, Python, outside `StudyHub.slnx`) | `requirements.txt` (`anki==26.9.3`). Package `anki_bridge/`: `config.py` (env vars); `snapshot.py` (consistent copy incl. `-wal`); `due_counts.py` (open the snapshot, `deck_due_tree()`, flatten to full names; the tree mapping is a pure function); `server.py` (`/health`, `/due-today`, JSON errors, one lock so snapshots never run in parallel); `__main__.py`. Tests in `tests/` (`unittest`). |
| **Anki sync server** | No own code. Uses the `anki` package's `python -m anki.syncserver` from the same image. |
| Shared | `Anki/AnkiDeckDueCountsDto`, `Anki/AnkiCollectionDueCountsDto`, `Anki/AnkiBridgeUnavailableException`, `Anki/AnkiCollectionNotFoundException`, `Dashboard/AnkiStudyStatus` (enum), `Dashboard/AnkiStudyStatusDto`, `Configuration/AnkiBridgeOptions` |
| Domain | `Logic.Domain.Contract`: `IAnkiDueCardsCalculator` and the `AnkiDueCards` result record. `Logic.Domain`: `AnkiDueCardsCalculator` (`HasCardsToStudy`; decks to show = top level with due > 0, sorted by due descending, then name). |
| Business | `IDashboardOrchestrator.GetAnkiStudyStatusAsync`. `DashboardOrchestrator` gets `IAnkiDueCountsAccessor`, `IAnkiDueCardsCalculator` and `IOptions<AnkiBridgeOptions>`, which makes 6 dependencies (within `COD-006`). It returns `Disabled` without calling the bridge when the feature is off. DI registration of the calculator. |
| Integration | `Anki/IAnkiDueCountsAccessor`; `Anki/AnkiBridgeAccessor` (typed `HttpClient`); internal wire records for the bridge JSON (one type per file, `COD-010`); `Anki/ServiceCollectionExtensions.AddStudyHubAnki(IConfiguration)` (options binding + `ValidateOnStart`, `COD-003`), called from the **Api** composition root only. `Dashboard/IDashboardAccessor` + `DashboardAccessor` get `GetAnkiStudyStatusAsync`. |
| Api | `DashboardController`: `[HttpGet("anki-status")]`. `Program.cs`: `AddStudyHubAnki(...)`. `appsettings.json`: `AnkiBridge` section (no secrets). `launchSettings.json`: local `BaseAddress` override (`appsettings.Development.json` is gitignored). |
| UI | `Components/Dashboard/AnkiStatusCard.razor` + `.razor.cs` (presentation only), CSS in `wwwroot/css/app.css`. `Dashboard.razor(.cs)`: load the Anki status independently; the card sits below the hero card. |
| Data / Infrastructure | No changes |
| Deployment | `Dockerfile`: new target `final-anki` (Python + pinned `anki` + bridge code). `docker-compose.yml`: services `anki-sync` and `studyhub-anki`, plus `AnkiBridge__BaseAddress` for `studyhub-api`. `.gitignore`: `.env`, `data/`, `__pycache__/`, `.venv/`. |

**Reuse from #22.** Its head (`6f63b79`) can still be fetched with `git fetch origin pull/22/head`.
These parts are adapted, not rewritten:

- the Domain calculator (totals now come from the bridge; the top-level rule stays)
- the orchestrator method (new status values)
- `AnkiStatusCard` (new states and the "as of" line)
- the independent loading in `Dashboard.razor.cs`, and the CSS
- the `DashboardAccessor` and `DashboardController` extensions
- the matching tests

Everything specific to AnkiConnect is replaced: `AnkiConnectAccessor` and its envelope types,
`AnkiConnectOptions`, `extra_hosts`, and the README section.

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

### Environment variables

| Service | Variable | Value / default | Purpose |
| --- | --- | --- | --- |
| `anki-sync` | `SYNC_USER1` | `${ANKI_SYNC_USERNAME}:${ANKI_SYNC_PASSWORD}` from `.env` | Sync login |
| `anki-sync` | `SYNC_BASE` | `/anki-sync` | Storage (bind mount `./data/anki-sync`) |
| `anki-sync` | `SYNC_HOST` / `SYNC_PORT` | `0.0.0.0` / `8080` | Listen address inside the container |
| `studyhub-anki` | `ANKI_COLLECTION_PATH` | `/anki-sync/${ANKI_SYNC_USERNAME}/collection.anki2` | Collection to read (read-only mount) |
| `studyhub-anki` | `ANKI_BRIDGE_HOST` / `ANKI_BRIDGE_PORT` | `0.0.0.0` / `8080` | Bind address (`127.0.0.1` for local runs) |
| `studyhub-anki` | `TZ` | `Europe/Berlin` | Defines Anki's "today" |

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
    /// <exception cref="AnkiCollectionNotFoundException">No collection at the configured path (nothing synced yet).</exception>
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

### 7.1 Api (`appsettings.json`, no secrets)

```json
"AnkiBridge": {
  "Enabled": true,
  "BaseAddress": "http://studyhub-anki:8080/",
  "TimeoutSeconds": 15
}
```

### 7.2 `Dockerfile` and `docker-compose.yml`

The `Dockerfile` gets one new, independent target; the existing targets stay unchanged:

```dockerfile
FROM python:3.12-slim-bookworm AS final-anki
# anki 26.9.3 ships manylinux_2_35 wheels (glibc >= 2.35); tzdata so TZ resolves Anki's day boundary.
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY src/AnkiBridge/requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY src/AnkiBridge/anki_bridge/ anki_bridge/
EXPOSE 8080
```

`docker-compose.yml` (new services; `studyhub-api` only gets `AnkiBridge__BaseAddress` if it
differs from `appsettings.json`):

```yaml
  anki-sync:
    build:
      context: .
      dockerfile: Dockerfile
      target: final-anki
    command: ["python", "-m", "anki.syncserver"]
    environment:
      - SYNC_USER1=${ANKI_SYNC_USERNAME:?set ANKI_SYNC_USERNAME in .env}:${ANKI_SYNC_PASSWORD:?set ANKI_SYNC_PASSWORD in .env}
      - SYNC_BASE=/anki-sync
      - SYNC_HOST=0.0.0.0
      - SYNC_PORT=8080
    volumes:
      - ./data/anki-sync:/anki-sync
    ports:
      - "127.0.0.1:27701:8080"   # only the host's HTTPS reverse proxy reaches it (section 7.3)
    restart: unless-stopped

  studyhub-anki:
    build:
      context: .
      dockerfile: Dockerfile
      target: final-anki
    command: ["python", "-m", "anki_bridge"]
    user: "nobody"
    environment:
      - TZ=Europe/Berlin
      - ANKI_COLLECTION_PATH=/anki-sync/${ANKI_SYNC_USERNAME}/collection.anki2
    volumes:
      - ./data/anki-sync:/anki-sync:ro
    expose:
      - "8080"
    healthcheck:
      test: ["CMD", "python", "-c", "import urllib.request; urllib.request.urlopen('http://localhost:8080/health')"]
      interval: 10s
      timeout: 3s
      retries: 5
    restart: unless-stopped
```

- `studyhub-api` gets no `depends_on` on the Anki services. The Api has to start without them, and
  an unreachable bridge simply shows up as `Unavailable`.
- `.env` on the server (never committed) holds `ANKI_SYNC_USERNAME` and `ANKI_SYNC_PASSWORD`. Use a
  long, random password, because the sync server is reachable from the internet.
- Running the bridge locally without Docker is documented in the README:
  1. `python -m venv .venv`
  2. `pip install -r requirements.txt`
  3. `ANKI_COLLECTION_PATH=… ANKI_BRIDGE_HOST=127.0.0.1 ANKI_BRIDGE_PORT=5260 python -m anki_bridge`

  The Api's `launchSettings.json` sets `AnkiBridge__BaseAddress=http://localhost:5260/`.

### 7.3 Prerequisite: HTTPS for the sync server

The phone syncs from anywhere, so `anki-sync` must be reachable from the internet. It must only be
reachable over **HTTPS**, because the login and the whole collection go over this connection.

- If the Strato server already runs a reverse proxy with TLS (nginx, Caddy, Traefik, Plesk) for
  StudyHub, a subdomain such as `anki.<domain>` gets a proxy rule to `127.0.0.1:27701`.
- If it does not, a small, separate deployment step adds one. A Caddy container in
  `docker-compose.yml` with automatic Let's Encrypt certificates needs a (sub)domain whose DNS
  points to the server, and ports 80/443 open.

## 8. Task Checklist

### Anki bridge (Python)

1. Project skeleton `src/AnkiBridge/` with `requirements.txt` (`anki==26.9.3`).
2. `snapshot.py`: consistent copy of `collection.anki2` + `-wal` with the stat check and retry;
   returns `collection_not_found` / `collection_unreadable`.
3. `due_counts.py`: open the snapshot, `deck_due_tree()`, totals from the root, flatten the tree
   into full names (`Parent::Child`) with `level`, `collectionModifiedAt`.
4. `server.py` + `__main__.py`: routes, JSON responses, error codes, logging, request lock.
5. `Dockerfile` target `final-anki`.

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
    - highlighted when there is something to study
    - "All done for today" when nothing is due
    - "as of <time>"
    - muted hints for `Unavailable` ("Anki bridge not reachable") and `CollectionNotFound`
      ("No Anki data yet – sync Anki with your StudyHub sync server")
    - hidden when `Disabled`
13. `Dashboard.razor(.cs)`: load the card independently of the semester progress.

### Deployment / Docs

14. `docker-compose.yml`: `anki-sync` and `studyhub-anki`.
15. `.gitignore`: `.env`, `data/`, `__pycache__/`, `.venv/`.
16. `README.md`:
    - the "Configure Anki" section (`.env`, HTTPS prerequisite, local run without Docker)
    - the migration guide from AnkiWeb (4.8) incl. switching back
    - a note to update the `anki` pin together with the Anki apps
17. `docs/agent-context.md`: the Anki services as the first out-of-process integration
    components; glossary entries.

## 9. Validation Plan

- `dotnet build StudyHub.slnx` with no new warnings, and `dotnet test StudyHub.slnx` green.
- `python -m unittest discover -s src/AnkiBridge/tests -t src/AnkiBridge` green.

**Python tests** run against the real `anki` library, using temp collections the tests create
themselves (no user data):

- `due_counts`:
  - totals from the root
  - full names and levels of subdecks
  - the daily new-card limit is respected (25 new → 20)
  - an empty collection gives zeros
- `snapshot`:
  - copying works while another `Collection` holds the original open (WAL included)
  - a change during copying leads to a retry and finally `collection_unreadable` (simulated)
  - a missing file gives `collection_not_found`
  - the temp directory is always removed
- **End-to-end**, the same as the spike:
  1. start `anki.syncserver` on a temp `SYNC_BASE`
  2. a client collection does a full upload, then studies 2 cards and syncs normally
  3. the bridge reading the server's file reports the client's counts
- `server`: `/health`, the 200 shape of `/due-today`, the 503 error codes (with a fake reader).

**.NET tests** (xUnit + Moq, no real bridge):

- `AnkiDueCardsCalculatorTests`:
  - `HasCardsToStudy` true/false
  - only top-level decks with due > 0
  - sort order
- `DashboardOrchestratorTests`:
  - `Disabled` returns without calling the accessor
  - mapping to `Unavailable` and `CollectionNotFound`
  - mapping to `Available`, incl. `CollectionModifiedAt`
- `AnkiBridgeAccessorTests` (fake `HttpMessageHandler`):
  - JSON parsing
  - 503 + `collection_not_found` → `AnkiCollectionNotFoundException`
  - other 503 codes, connection failure and timeout → `AnkiBridgeUnavailableException`
- `DashboardEndpointsTests`: `GET api/dashboard/anki-status` → 200 with the orchestrator's DTO.

**Manual checks** with `docker compose up`, locally and then on the Strato server:

- Before the first sync: hint "No Anki data yet".
- After the desktop upload: the counts match Anki's deck browser.
- After studying on the phone and syncing: reloading the Dashboard shows lower counts.
- Bridge stopped: "not reachable". The semester card is unaffected.
- Time zone: between midnight and 04:00 the counts still belong to the previous Anki day, as in
  the apps.

## 10. Open Questions, Risks, Assumptions

### Answered (2026-10-07)

- **Deployment:** StudyHub runs on a Strato server. The user syncs with AnkiWeb today, which led to
  revision 2 with the own sync server.
- **Time zone:** `Europe/Berlin` confirmed.
- **Freshness:** acceptable that the counts show the state of the last sync.
- **Approvals:** the new dependency `anki` plus a Python runtime (`DEP-001`) and the changes to
  `Dockerfile` / `docker-compose.yml` (`SCP-001`) are approved.
- **Secrets:** never in the repo. Locally they go into `dotnet user-secrets` (where .NET needs
  them); on the server they go into `.env`. This feature's only secret, the sync password, lives
  in the server's `.env` (4.9).

### Still open (please answer before implementation)

1. **Switching away from AnkiWeb:** are you willing to point Anki desktop and your phone app at your
   own sync server instead of AnkiWeb (4.8)? This is the core decision. Without it, there is no
   permitted way to get the data onto the server while Anki desktop is not running.
2. **Strato server:** is it a VPS / root server with Docker (StudyHub needs that anyway)? Is
   StudyHub already reachable over **HTTPS with a domain** (which reverse proxy?), and can a
   subdomain like `anki.<domain>` be added (7.3)?
3. **Phone app:** AnkiDroid or AnkiMobile? AnkiMobile needs version 2.0.90 or newer for a custom
   server.
4. **License:** this is not a key or setting, only a question of terms. `anki` is licensed under
   AGPL-3.0, and the bridge imports it. Because the repository is **public**, the bridge code
   should carry an AGPL-3.0 license text (`src/AnkiBridge/LICENSE`). StudyHub's .NET code only talks
   to the bridge over HTTP and is unaffected. OK?

### Risks and assumptions

- **Moving off AnkiWeb:** the user becomes responsible for the sync server's availability and its
  backups. Mitigations:
  - include `data/anki-sync` in the server backup
  - AnkiWeb keeps the old state
  - Anki desktop keeps its own automatic backups
- **Internet exposure:** the sync server is reachable from the internet. Mitigations: HTTPS only,
  a long random password, and only `127.0.0.1:27701` published on the host (the reverse proxy is
  the only entry point).
- **Version compatibility:** the server and the bridge share one pinned `anki` version. The apps
  should not be much newer than the server; when Anki desktop gets a major update, update the pin
  too. A file the library cannot read shows up as `Unavailable`, and the bridge log names the
  cause.
- **Reliance on the sync server's storage layout:** `SYNC_BASE/<user>/collection.anki2` is an
  implementation detail, not a public API. Because server and bridge use the same pinned version,
  the end-to-end test catches layout changes when the pin is updated.
- **Snapshot consistency:** the stat check detects a sync running during the copy and retries. In
  the worst case one Dashboard load shows "Unavailable"; it never shows wrong numbers silently and
  never touches the original.
- **Assumption:** "the open cards for the respective day" means Anki's **due today** counts
  (new + learning + review, with daily limits). A forecast for upcoming days is a follow-up.
- UI strings are hardcoded, like the rest of the current Dashboard. `UIX-002` is not addressed
  because no localization infrastructure exists yet.

### Documentation inconsistencies found while planning

These are reported per `CLAUDE.md`, not resolved by guessing:

1. **`ARC-006`:** integration folder placement is still an open decision. This plan places the
   first out-of-process component at `src/AnkiBridge/`, a self-contained Python project with its
   tests next to it, outside `StudyHub.slnx`. Please confirm.
2. **`COD-001`:** the rule says no automatic `Async` suffix, but the existing code uses `Async`
   throughout. This plan follows the existing code.
3. **`TST-001` / `TST-007`:** they require FluentAssertions, but `StudyHub.Tests` uses xUnit
   `Assert`. This plan follows the existing tests.
4. **README vs. `.gitignore`:** the README describes `data/` as "gitignored", but `.gitignore` only
   ignores `*.db*` files. This plan adds `data/` and `.env` (4.9).

## 11. Follow-ups (not part of this feature)

- A bridge-side cache keyed by collection mtime/size and the Anki day (4.7).
- A forecast for the next days (`prop:due=N` searches) and a deck → Course mapping.
- Feeding the real Anki numbers into the placeholder stat tiles ("Flashcards reviewed this week")
  and into Learning Analytics.
- Optional: pushing cards generated in StudyHub straight into the collection over the own sync
  server, instead of exporting a CSV. That would be a write path and needs its own plan.
