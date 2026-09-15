# Feature Plan: Markdown Notes

Status: Draft
Classification (per `CLAUDE.md`): **Medium** (new CRUD feature, new domain object, new page —
implements the existing `/notes` stub page). Per the Medium workflow, this document covers: the
planned architecture, the affected layers, the required contracts, and the database impact.

## 0. Documentation Note

`CLAUDE.md` says: "If documentation conflicts with the current implementation, report the
inconsistency instead of making assumptions." Two docs in this repo do not describe StudyHub at
all — `docs/architecture.md` (a generic "Polipol.PA" company standard with `Polipol.PA.*`
namespaces, `1_CrossCutting`/`2_Data`/... solution folders, per-aggregate `GetForMutation`
repository conventions, etc.) and `docs/agent-context.md` (describes an unrelated
"Polipol.AppLauncher" codebase with `Frontend`/`Backend`/`DataStoring` projects). Neither matches
this repository's actual layout (`Shared`/`Data`/`Logic.{Domain,Business,Integration}`
/`Infrastructure`/`Api`/`UI`, as described in `CLAUDE.md` and `README.md`, and as already
implemented for Courses/Semesters/Documents). This plan follows the **actual codebase
conventions** (mirroring the existing `Documents` feature and its own plan,
`docs/plans/document-management-plan.md`), not `docs/architecture.md`/`docs/agent-context.md`.
Flagging this rather than silently ignoring it, per the instruction above.

## 1. Goal

Implement the `/notes` page (currently a stub) so a student can write, edit, and organize
Markdown notes. Each note is assignable to exactly one `Course` or one `Semester`, and can be
linked to zero or more existing `Document`s (reusing the Document Management feature already in
place — no new upload mechanism is introduced).

## 2. Scope

In scope:

- Create, edit, and delete (soft-delete/archive) a Markdown note.
- Assign a note to exactly one `Course` or one `Semester`.
- Link a note to zero or more existing, already-uploaded `Document`s.
- List notes (all / by course / by semester), with a search box and an archived filter, mirroring
  the existing `Documents.razor` toolbar pattern.
- A split editor: Markdown source on one side, rendered preview on the other (per the existing
  mockup, `docs/mockup/StudyHub.html`, function `pageNotes()`).

Out of scope (future milestones per `README.md`'s roadmap / `docs/roadmap.md`'s "Markdown Notes"
item and beyond):

- Tags (`docs/mockup/StudyHub.html` shows them; the current domain has no tagging concept
  anywhere yet — Documents don't have tags either. Adding a cross-cutting tag model is a separate
  concern and would touch both features; left out of this plan).
- Full-text search across notes.
- AI-assisted summarization/flashcard generation from notes (Milestone 9/10 in the roadmap).
- Versioning/history of note edits.
- Uploading a *new* file directly from the note editor (a note only links to documents already
  uploaded via the Documents page).

## 3. Decisions

- **Association: a note belongs to either a `Course` or a `Semester`, never both, never
  neither.** Same shape as `Document` today: `Note` gets two nullable FKs (`CourseId`,
  `SemesterId`) with the invariant enforced in the domain constructor/update method *and* as a DB
  check constraint (mirrors `Document`'s `CK_Documents_ExactlyOneParent`).

- **Linking to Documents: many-to-many via a plain join table (`NoteDocuments`), not a new rich
  domain type.** A link is just an association with no lifecycle or behavior of its own (unlike
  `Note` or `Document`, which have state transitions like `Archive()`). `Note` exposes its linked
  documents as a read-only `IReadOnlyList<Guid> LinkedDocumentIds`, backed by a private collection
  mapped by EF Core to the `NoteDocuments` table (composite key `NoteId`+`DocumentId`). No
  reciprocal navigation is added to `Document` for v1 (a document doesn't need to know which notes
  reference it yet) — smallest useful solution; can be added later if needed (e.g. a "used in N
  notes" badge on the Documents page).

- **Replace-all-links on save, not incremental link/unlink endpoints.** `CreateNoteRequest` and
  `UpdateNoteRequest` both carry the full desired `DocumentIds` set; `NoteManagement` diffs
  against the current set and persists the result. Simpler contract than
  `LinkDocumentAsync`/`UnlinkDocumentAsync`, and the UI's document picker naturally works with a
  full selected-set model (checkbox list, same interaction as the mockup's "Document Context"
  picker in the AI page).

- **Content storage: `Content` as an unbounded SQLite `TEXT` column, capped at 200,000
  characters (~200 KB) enforced in the domain layer** — generous for typical lecture/exam notes,
  while still guarding against unbounded row growth (same spirit as `Document`'s 25 MB upload
  cap, scaled down because this is plain text a user types, not an arbitrary uploaded file).
  Assumption — flag if a different limit is wanted.

- **Markdown rendering: add the [Markdig](https://github.com/xoofx/markdig) NuGet package**
  (MIT-licensed, the de facto standard .NET Markdown processor) to `StudyHub.UI` for the live
  preview pane. This is a new third-party dependency, which `CLAUDE.md`'s Medium workflow calls
  out to flag explicitly. The pipeline is configured **without** raw-HTML passthrough (Markdig's
  default disables raw HTML unless `.UseAdvancedExtensions()`/`DisableHtml()` says otherwise; we
  keep raw HTML disabled) to avoid persisted script injection into the preview, since notes are
  free-form user text.
  - Preview updates happen server-side (Blazor Interactive Server, same render mode already used
    everywhere): each edit round-trips to the server and re-renders the `MarkupString`, same cost
    class as the `@bind:after` pattern already used in `Documents.razor`'s scope selector.

- **Soft delete**, mirroring `Course`/`Semester`/`Document`: a `Note.IsArchived` flag with
  `Archive()`/`Restore()` domain methods and a `NoteArchivedException` (same shape as
  `DocumentArchivedException`). List queries return archived notes too; filtering is a UI concern
  (`ShowArchived` toggle, same as `Documents.razor`).

- **No tags/search-by-tag in v1** (see Scope). The list view gets a simple client-side title/body
  substring search box instead (no new backend endpoint needed — `NoteManagement.GetAllAsync`/
  `GetByCourseIdAsync`/`GetBySemesterIdAsync` already return everything needed for the selected
  scope, matching how `Documents.razor` already filters client-side for `ShowArchived`).

## 4. Architecture Impact

Following the project's actual layering (`Shared` / `Data` / `Logic.{Domain, Business,
Integration}` / `Infrastructure` / `Api` / `UI`), mirrored on the existing `Documents` feature:

| Layer | Project | Additions |
|---|---|---|
| Domain | `StudyHub.Logic.Domain` | `Notes/Note.cs` (entity with `Create`/`Update`/`Archive`/`Restore`/`SetLinkedDocuments`, mutually-exclusive `CourseId`/`SemesterId`, private `LinkedDocumentIds` backing collection), `INoteRepository`, `NoteNotFoundException`, `NoteValidationException`, `NoteArchivedException` |
| Business | `StudyHub.Logic.Business` | `Notes/INoteManagement.cs`, `NoteManagement.cs` (validates parent existence/not-archived like `DocumentManagement`, validates linked document ids exist), `NoteDto`, `CreateNoteRequest`, `UpdateNoteRequest`, `NoteErrorCodes` |
| Infrastructure | `StudyHub.Infrastructure` | `Notes/NoteRepository.cs` (EF Core repository; loads/saves the `NoteDocuments` join rows alongside the note) |
| Data | `StudyHub.Data` | `DbSet<Note>` + fluent configuration in `OnModelCreating` (new `Notes` table + `NoteDocuments` join table + check constraint), one EF Core migration |
| Api | `StudyHub.Api` | `Notes/NoteEndpoints.cs` (CRUD + archive/restore), `NoteExceptionHandler.cs` (mirrors `DocumentExceptionHandler.cs`) |
| UI | `StudyHub.UI` | `Notes/NoteApiClient.cs` (HTTP adapter satisfying `INoteManagement`, same pattern as `DocumentApiClient`), rewritten `Components/Pages/Notes.razor` (+ code-behind) implementing the split list/editor/preview layout, a `NoteEditorDialog`-equivalent or inline editor, a document-picker component for linking |
| Tests | `StudyHub.Tests` | Business-logic tests for `NoteManagement` (create/update validation, exactly-one-parent, linked-document-id validation, archive/restore, mapping) |

Contracts-first: `UI` calls only `INoteManagement` (Business, over HTTP via `NoteApiClient`, same
as `DocumentApiClient`/`CourseApiClient`/`SemesterApiClient` today); `Business` calls
`INoteRepository` (Domain) plus the existing `ICourseRepository`/`ISemesterRepository` (to
validate the assigned parent) and `IDocumentRepository` (to validate linked document ids exist
and are not archived, same check `DocumentManagement` already does for its own parents);
`Infrastructure` implements `INoteRepository`.

## 5. Database Impact

- New table `Notes`: `Id`, `Title` (`nvarchar`/`TEXT`, max length 200), `Content` (`TEXT`, capped
  at 200,000 chars at the domain layer), `CourseId` (nullable FK, `Restrict` delete — same
  reasoning as `Document` → `Course`/`Semester` today), `SemesterId` (nullable FK, `Restrict`),
  `IsArchived`, `CreatedAt`, `UpdatedAt`.
- New join table `NoteDocuments`: composite PK (`NoteId`, `DocumentId`), FK `NoteId` → `Notes`
  (`Cascade` — join rows have no meaning without their note and notes are only ever
  soft-deleted/archived in the UI, so this only matters for the rare manual/administrative hard
  delete), FK `DocumentId` → `Documents` (`Restrict`, consistent with the rest of the schema never
  silently cascading through a primary entity).
- Check constraint enforcing exactly one of `CourseId`/`SemesterId` is set on `Notes`
  (`CK_Notes_ExactlyOneParent`, same pattern as `CK_Documents_ExactlyOneParent`).
- Indexes on `Notes.CourseId` and `Notes.SemesterId` for listing by parent; index on
  `NoteDocuments.DocumentId` for the (currently unused but cheap to have) reverse lookup.
- One EF Core migration (`AddNote`), per `CLAUDE.md` "one migration per feature". No provider
  change — stays on the existing SQLite database.

## 6. Task Checklist

### Backend

- [ ] `Note` domain entity — mutually-exclusive `CourseId`/`SemesterId`, `Content` length
      validation, `IsArchived` + `Archive()`/`Restore()`, linked-document-id collection with
      `SetLinkedDocuments(IEnumerable<Guid>)`.
- [ ] `INoteRepository` + `NoteRepository` (EF Core) + `OnModelCreating` configuration + migration
      (`Notes` + `NoteDocuments` tables, check constraint, indexes).
- [ ] `NoteManagement` — create/update validate parent existence & not-archived (reusing
      `ICourseRepository`/`ISemesterRepository`, same checks `DocumentManagement` already has) and
      validate every linked document id exists (via `IDocumentRepository`); archive/restore;
      mapping to `NoteDto`.
- [ ] `NoteEndpoints` (`GET /api/notes`, `GET /api/notes/by-course/{id}`,
      `GET /api/notes/by-semester/{id}`, `GET /api/notes/{id}`, `POST /api/notes`,
      `PUT /api/notes/{id}`, `POST /api/notes/{id}/archive`, `POST /api/notes/{id}/restore`) +
      `NoteExceptionHandler`.

### UI

- [ ] `NoteApiClient` implementing `INoteManagement` over HTTP (same shape as
      `DocumentApiClient`).
- [ ] `Notes.razor` — list/editor split layout (per the mockup): note list with search box, scope
      selector (all/course/semester) and archived toggle on the left; Markdown editor + live
      preview pane on the right.
- [ ] Note editor: title field, course/semester picker (mutually exclusive, same UX as the
      existing course/semester forms), Markdown textarea, Markdig-rendered preview pane.
- [ ] Document-linking picker: checkbox list of existing (non-archived) `Document`s, defaulting to
      the note's current course/semester scope, reusing `DocumentDto`s already fetched the way
      `Documents.razor` preloads `Courses`/`Semesters`.
- [ ] Add Markdig package reference to `StudyHub.UI.csproj`.

## 7. Validation Plan

- `dotnet build`
- `dotnet test`
- Manual verification in the running Blazor UI: create a note under a course, link two existing
  documents, edit content and confirm the preview updates, switch the note to a semester instead,
  archive and restore it, and confirm the list/scope filters and search box behave as expected.

## 8. Risks / Assumptions

- 200 characters for `Title` and 200,000 characters for `Content` are assumed caps, not values
  the user confirmed — flag if different limits are wanted.
- Markdig is a new third-party dependency (none of the existing features need a Markdown
  processor yet); flagged above per the Medium-workflow "identify required contracts" step. If a
  different rendering approach is preferred (e.g. a client-side JS library instead of server-side
  Markdig), that changes the UI task list but not the Domain/Business/Data design.
- No tags in v1 despite the mockup showing them — see Scope. If tags are actually wanted now
  (not deferred), that's a small addition (a `Tags` string collection on `Note`, no new table
  needed for a first cut) but changes the Database Impact section, so calling it out before
  implementation rather than assuming.
- The `NoteDocuments` join uses `Cascade` on the `NoteId` FK while everything else in the schema
  uses `Restrict` — this is intentional (see Database Impact) since it only affects join rows,
  not a primary entity, but worth a second look during implementation review since it's the first
  `Cascade` in the schema.
