# Feature Plan: Markdown Notes

Status: Draft — awaiting review/confirmation before implementation
Classification (per `CLAUDE.md`): **Medium** (new CRUD feature, new domain object,
new page, new business workflow). Per the Medium workflow this plan covers
planned architecture, affected layers, required contracts, and database
impact — implementation starts only after review.

## 0. Documentation note

`CLAUDE.md` asks to flag doc/implementation conflicts instead of guessing, so
two are noted upfront (not blockers, just context for this plan):

- `docs/architecture.md` and `docs/agent-context.md` describe a different,
  unrelated codebase (a "Polipol.AppLauncher"/"CoCo" WPF+Postgres solution
  with `.Contract`/`.Implementation` project pairs). None of it matches this
  repository (StudyHub, a Blazor Web App on the simpler
  `Shared/Data/Logic{Domain,Business,Integration}/Infrastructure/UI/Tests`
  layout `CLAUDE.md` itself describes). This plan follows the actual
  repository structure and the precedent set by
  `docs/plans/document-management-plan.md`, not those two files.
- `CLAUDE.md` references `docs/roadmap.md` and `docs/adrs/`; neither exists
  in the repo (only `docs/architecture.md`, `docs/agent-context.md`,
  `docs/agent-rule-catalog.md`, `docs/mockup/`, `docs/plans/`). The
  "Markdown Notes" milestone name is taken from `CLAUDE.md`'s own "Long-Term
  Vision" list and from `docs/mockup/StudyHub.html`, which already contains a
  full Notes page mockup (`pageNotes()`, `.notes-layout` CSS, sample data).

## 1. Goal

Implement the existing `Notes` Razor page (`src/UI/StudyHub.UI/Components/Pages/Notes.razor`,
currently a "coming soon" stub) as a real Markdown notes feature: create,
edit, archive/restore, and browse notes, each one assigned to exactly one
Semester or one Course, with the ability to link one or more previously
uploaded Documents to a note.

## 2. Scope

In scope:

- CRUD for notes: title, Markdown content, optional tags, created/updated
  timestamps.
- A note belongs to exactly one `Course` or one `Semester` (mirrors the
  existing `Document` invariant).
- Attaching/detaching existing `Document`s to a note (reusing the Document
  feature already implemented — no new file-storage code).
- Implementing `Notes.razor` per the existing mockup (`docs/mockup/StudyHub.html`,
  `pageNotes()`): two-pane layout — note list (search + tag filter) on the
  left, Markdown editor with live preview on the right.
- Soft delete (archive/restore), mirroring `Course`/`Semester`/`Document`.

Out of scope (future milestones per `CLAUDE.md`'s "Long-Term Vision"):

- AI summarization / flashcard generation from notes.
- Rich-text/WYSIWYG editing, image embedding, or drag-drop file upload
  directly inline in the Markdown body.
- Full-text search ranking (a simple client-side substring filter over
  title/content/tags is in scope; a search index is not).
- Real-time collaborative editing.

## 3. Decisions (flagged for confirmation)

- **Association: exactly one of `CourseId` / `SemesterId`.** Same
  mutual-exclusivity invariant as `Document` (enforced in the domain
  constructor and as a DB check constraint) — keeps the mental model
  consistent across the two "attachable to Course-or-Semester" features.
- **Attachments are a many-to-many link to existing `Document`s, not a new
  upload path.** A join table `NoteDocuments` (`NoteId`, `DocumentId`,
  composite key) records the link. Uploading a *new* file from within the
  note editor is supported by re-using the existing
  `DocumentUploadDialog` component (pre-filled with the note's
  Course/Semester) and then auto-attaching the resulting document — no new
  upload/storage code, per "avoid unnecessary abstractions". **Assumption to
  confirm:** an attached document does not have to belong to the same
  Course/Semester as the note (kept unrestricted for simplicity); flag if
  that should be enforced.
- **Tags as a single delimited string column** (e.g. comma-separated,
  parsed/joined at the Business layer into `IReadOnlyList<string>` on the
  DTO), not a separate `Tag` entity/table. The mockup only ever needs simple
  tag chips and an "all tags" filter — a normalized tag table would be
  over-engineering for that. **Assumption to confirm:** no cross-note tag
  management (rename/merge tags) is required now.
- **No EF navigation properties**, consistent with the rest of the codebase
  (`Course`/`Semester`/`Document` only expose scalar FK `Guid`/`Guid?`
  properties, no `.Course` / `.Documents` collections, and `CLAUDE.md`
  says "Avoid lazy loading"). Attachment lookups go through explicit
  repository methods, not a navigation collection on `Note`.
- **Markdown rendering needs a new dependency.** No Markdown library is
  currently referenced anywhere in the solution. Proposal: add
  **Markdig** (the standard, actively maintained .NET Markdown-to-HTML
  library) to `StudyHub.UI` only, used purely for the live preview pane.
  Render with Markdig's default (non-raw-HTML) pipeline so a note's Markdown
  can't inject arbitrary HTML/script into the preview — notes are
  single-user content today, but there's no reason to allow HTML passthrough.
  **Flagging this because it's the one new external dependency this feature
  needs; please confirm before it's added.**
- **Content size cap:** 50,000 characters per note (assumption, mirrors the
  spirit of `Document`'s 25 MB upload cap — flag if a different limit is
  wanted).
- **Note creation/editing UX departs from the Course/Semester/Document modal
  pattern.** The mockup shows notes edited inline in the right-hand pane, not
  in a popup dialog, so "New Note" opens the editor pane in create mode
  rather than opening a `NoteFormDialog`. This is a deliberate deviation from
  the modal convention used elsewhere, justified by the mockup's own UX and
  by Markdown editing needing more room than a modal comfortably gives.

## 4. Architecture Impact

Mirrors the existing `Documents`/`Courses`/`Semesters` features exactly
(contracts-first: `UI` → `Business` contract → `Domain` contract →
`Infrastructure` implementation):

| Layer | Project | Additions |
|---|---|---|
| Domain | `StudyHub.Logic.Domain` | `Notes/Note.cs` (entity: `Title`, `Content`, `Tags`, `CourseId`/`SemesterId`, `IsArchived`, `Archive()`/`Restore()`, `Update()`), `INoteRepository` (CRUD + `GetAttachedDocumentIdsAsync`/`AddAttachmentAsync`/`RemoveAttachmentAsync`), `NoteNotFoundException`, `NoteValidationException`, `NoteArchivedException` |
| Business | `StudyHub.Logic.Business` | `Notes/INoteManagement.cs`, `NoteManagement.cs` (CRUD, `ArchiveAsync`/`RestoreAsync`, `AttachDocumentAsync`/`DetachDocumentAsync` — depends on `INoteRepository` + `IDocumentRepository` + `ICourseRepository` + `ISemesterRepository`, same "Business depends on Domain repository contracts directly" pattern `DocumentManagement` already uses), `NoteDto` (incl. `AttachedDocumentIds`), `CreateNoteRequest`, `UpdateNoteRequest`, `NoteErrorCodes` |
| Infrastructure | `StudyHub.Infrastructure` | `Notes/NoteRepository.cs` (EF Core repository incl. `NoteDocuments` join queries) |
| Data | `StudyHub.Data` | `DbSet<Note>` + `DbSet<NoteDocument>`, `OnModelCreating` configuration (mirrors `Document`'s check-constraint pattern), one EF Core migration |
| UI (API) | `StudyHub.Api` | `Notes/NoteEndpoints.cs` (list/by-course/by-semester/by-id, create, update, archive, restore, attach/detach document), `NoteExceptionHandler.cs` |
| UI (Blazor) | `StudyHub.UI` | `Notes/NoteApiClient.cs`, rewritten `Components/Pages/Notes.razor` (+ code-behind) with note list + Markdown editor/preview, `Components/Shared/NoteAttachmentPicker.razor` (reuses `DocumentUploadDialog` + existing `DocumentDto` list) |
| Tests | `StudyHub.Tests` | `Logic/Domain/Notes/NoteTests.cs`, `Logic/Business/Notes/NoteManagementTests.cs`, `Infrastructure/Notes/NoteRepositoryTests.cs`, `Api/Notes/NoteEndpointsTests.cs` |

DI wiring (mirrors `Document` exactly):

- `StudyHub.Infrastructure/ServiceCollectionExtensions.cs`: register `INoteRepository → NoteRepository`.
- `StudyHub.Logic.Business/ServiceCollectionExtensions.cs`: register `INoteManagement → NoteManagement`.
- `StudyHub.UI/Program.cs`: `builder.Services.AddHttpClient<INoteManagement, NoteApiClient>(...)`.

## 5. Database Impact

- New table `Notes`: `Id`, `Title`, `Content` (capped at 50,000 chars),
  `Tags` (delimited string, nullable), `CourseId` (nullable FK, `Restrict`),
  `SemesterId` (nullable FK, `Restrict`), `IsArchived`, `CreatedAt`, `UpdatedAt`.
  Check constraint enforcing exactly one of `CourseId`/`SemesterId`, same as
  `Documents.CK_Documents_ExactlyOneParent`.
- New join table `NoteDocuments`: `NoteId` (FK → `Notes`, `Restrict`),
  `DocumentId` (FK → `Documents`, `Restrict`), composite primary key
  `(NoteId, DocumentId)`. `Restrict` on both sides for the same reason as
  elsewhere in the schema — notes and documents are only ever archived, never
  hard-deleted, so a physical delete must not silently cascade.
- Indexes on `Notes.CourseId`, `Notes.SemesterId`, and `NoteDocuments.DocumentId`
  (for "which notes reference this document" lookups).
- One EF Core migration ("AddNote"), per `CLAUDE.md`'s "one migration per
  feature". No provider change — stays on the existing SQLite database.

## 6. Task Checklist

### Backend

- [ ] `Note` domain entity + `INoteRepository` + EF configuration/migration.
- [ ] `NoteManagement` (create/update/archive/restore, attach/detach document,
      validation: title required, content size cap, exactly-one-parent,
      parent existence/not-archived — mirrors `DocumentManagement`).
- [ ] `NoteEndpoints` (list/by-course/by-semester/by-id, create, update,
      archive, restore, attach/detach document) + `NoteExceptionHandler`.

### UI

- [ ] `NoteApiClient` implementing `INoteManagement` over HTTP (mirrors
      `DocumentApiClient`, including `ProblemDetails` → exception mapping
      for `NoteErrorCodes` and the reused `Document`/`Course`/`Semester`
      error codes).
- [ ] `Notes.razor` rewrite: note list (search box, tag filter chips, scope
      filter reusing the `Documents.razor` course/semester `<select>`
      pattern) + Markdown editor pane with live preview (Markdig).
- [ ] "New note" / "attach document" flows per section 3.
- [ ] Archive/restore actions (mirrors `DocumentDetailsDialog`'s pattern).

## 7. Validation Plan

- `dotnet build`
- `dotnet test`
- Manual verification in the running Blazor UI: create a note under a
  Course, create one under a Semester, edit content and confirm the
  Markdown preview updates, attach an existing document, upload+attach a
  new document, archive/restore a note, filter by tag and by course/semester
  scope.

## 8. Risks / Assumptions

- All items under section 3 ("Decisions") are explicit assumptions —
  particularly the new Markdig dependency and the unrestricted
  note-to-document attachment scope — and should be confirmed before
  implementation starts.
- 50,000 characters and the tag-as-string approach are the two most likely
  candidates to need revisiting if usage patterns turn out different than
  expected (e.g. long lecture-note transcripts, or a desire to rename a tag
  across all notes at once).
- The two pre-existing documentation gaps noted in section 0 are unrelated to
  this feature and aren't addressed by this plan.
