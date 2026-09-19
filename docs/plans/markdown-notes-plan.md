# Feature Plan: Markdown Notes

Status: Implemented (CRUD, wiki-links, backlinks, outline, templates). See the
"Slash-command menu" addendum below (section 2) for a follow-up UI addition
requested after the initial implementation.
Classification (per `CLAUDE.md`): **Medium** (new CRUD feature, new domain object,
new page, new business workflow). Per the Medium workflow this plan covers
planned architecture, affected layers, required contracts, and database
impact — implementation starts only after review.

## 0. Documentation Cleanup (in scope)

`CLAUDE.md` asks to flag doc/implementation conflicts instead of guessing.
Two were found while researching this plan — per review feedback, both are
now **in scope** for this branch rather than just flagged:

- **`docs/architecture.md` and `docs/agent-context.md` describe a different,
  unrelated codebase** (a "Polipol.AppLauncher"/"CoCo" WPF+Postgres solution
  with `.Contract`/`.Implementation` project pairs, `Polipol.PA.*`/
  `Polipol.AppStore` namespaces, MSTest, WPF frontend). None of it matches
  this repository (StudyHub, a Blazor Web App on the simpler
  `Shared/Data/Logic{Domain,Business,Integration}/Infrastructure/UI/Tests`
  layout `CLAUDE.md` itself describes, using xUnit, EF Core/SQLite, ASP.NET
  Core minimal APIs). **Action:** rewrite both files so they describe
  StudyHub itself instead of being deleted or left stale — real repository
  map (`src/Shared`, `src/Data`, `src/Logic/{Domain,Business,Integration}`,
  `src/Infrastructure`, `src/UI/{StudyHub.Api,StudyHub.UI}`,
  `tests/StudyHub.Tests`), real components (Semesters/Courses/Documents/
  Notes), real DI pattern (one `ServiceCollectionExtensions` per project,
  not per-component `.Contract`/`.Implementation` pairs), real test stack
  (xUnit), real local-run commands. `docs/plans/document-management-plan.md`
  and this plan, together with the actual `src/` tree, are the source of
  truth for the rewrite — not invented content.
- **`CLAUDE.md` references `docs/roadmap.md` and `docs/adrs/`, neither of
  which exists** (only `docs/architecture.md`, `docs/agent-context.md`,
  `docs/agent-rule-catalog.md`, `docs/mockup/`, `docs/plans/`). **Action:**
  remove those two dangling references from `CLAUDE.md`'s "Documentation"
  section. (The "Markdown Notes" milestone name itself stays valid either
  way — it comes from `CLAUDE.md`'s own "Long-Term Vision" list and from
  `docs/mockup/StudyHub.html`'s existing `pageNotes()` mockup, neither of
  which depends on the missing files.)

This cleanup doesn't depend on the Notes feature code and is planned as its
own small first commit on this branch, before the feature work (see the
Task Checklist).

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
- **Obsidian-style wiki-links and backlinks** (per review feedback asking
  for Notion/Confluence/Obsidian-like basics — see section 3a for the full
  feature-by-feature reasoning): `[[Note Title]]` inside a note's Markdown
  resolves to a clickable link to that note, and each note shows a "Linked
  mentions" panel listing every other note that links to it.
- **Heading outline** in the editor pane — a small in-page table of contents
  generated from the note's own `#`/`##`/`###` headings. Cheap once Markdig
  parses the content for the preview anyway, and a direct Notion/Confluence
  affordance.
- **Note templates on creation** ("Lecture Notes", "Exam Prep", "Blank" —
  client-side starter snippets only, no schema change) — a lightweight
  Notion-style quick start.
- **Addendum — slash-command menu** (requested after the rest of this plan
  shipped: "wie in Confluence ... mit `/` Sachen wie Listen oder Code
  Snippets erstellen"). Typing `/` at the start of a line in the editor
  opens a filterable popup (Heading 1–3, Bulleted/Numbered/Task list, Quote,
  Code block, Table, Divider, Link); picking one deletes the `/query` text
  and inserts the corresponding Markdown snippet at the cursor. Implemented
  as a client-side text-snippet inserter over the existing plain-text
  `Content` field — **not** a block-per-node document model — so it needed
  no backend/contract/schema change: `Components/Shared/SlashCommand.cs` +
  `SlashCommandMenu.razor` (popup UI, keyboard nav) plus
  `wwwroot/js/notes-editor.js` (a small JS interop module; Blazor doesn't
  expose a `<textarea>`'s cursor pixel position or `selectionStart`, so
  those two need direct DOM access — same "one new client-side script"
  shape flagged for Markdown rendering below, just for the editor itself
  rather than the preview). See the "Rich-text/WYSIWYG" bullet just below
  for why this stays out of block-editor territory.
- **Addendum — code block syntax highlighting** (follow-up to the addendum
  above: "ungefähr wie bei confluence auch syntax highlighting"). The
  preview pane's fenced code blocks (` ```csharp ` etc.) are now colorized
  client-side. Markdig's default (CommonMark-spec) fenced-code rendering
  already emits `<pre><code class="language-csharp">` for a fenced block
  with an info string — no pipeline change needed — so this only needed a
  highlighter to walk that markup: **Prism.js** (MIT), vendored directly as
  a static file (`wwwroot/js/vendor/prismjs/prism-bundle.js` — core plus the
  csharp/java/python/javascript/typescript/sql/bash/json/markup(html)/css/
  yaml/go/rust/markdown grammars, fetched via `npm pack prismjs` and
  concatenated/minified). Lives under `wwwroot/js/vendor/`, not
  `wwwroot/lib/` — the latter is `.gitignore`d project-wide (populated at
  restore time from a static-web-assets NuGet package, the way the
  `bootstrap` PackageReference populates `lib/bootstrap/`; no such package
  exists for Prism, so committing it as a plain tracked file was simpler
  and safer than inventing one).
  `Prism.manual = true` (set in `App.razor` before the bundle loads) turns
  off its own DOMContentLoaded auto-highlight, and
  `notes-editor.js`'s new `highlightCode()` is called from
  `Notes.razor.cs`'s `OnAfterRenderAsync` instead, since the preview's HTML
  is replaced wholesale on every render (a `MarkupString` region) and Prism
  never sees those new `<code>` elements otherwise. Token colors are mapped
  to the app's own `--accent`/`--success`/`--warning`/`--text-2`/`--text-3`
  custom properties (plus one new `--code-function`) rather than shipping
  one of Prism's canned theme CSS files, so highlighted code stays
  consistent with StudyHub's own light/dark palette instead of looking like
  a foreign, hardcoded theme. The slash-command menu (addendum above) grew
  one "Code: `<language>`" entry per bundled language so users don't have to
  remember Prism's exact language identifiers.
- **Addendum — read-only view mode by default** ("das ganze soll im
  frontend nicht nach markdown aussehen, nur im backend"). Opening an
  existing note no longer drops straight into the raw-Markdown `<textarea>`;
  it shows the rendered page (title, course/semester, tags, updated date,
  outline, backlinks — the same `PreviewHtml`/`HeadingOutline` already built
  for the preview pane, just as the note's primary view instead of an
  opt-in split pane) with an **Edit** button, matching Confluence's
  view-first/edit-on-demand page model instead of a permanently-open source
  editor. Clicking Edit swaps in the existing raw-Markdown editor (full
  width, slash menu, optional live-split preview per the addendum above);
  Save/Cancel/Archive all return to the rendered view. An archived note has
  no Edit affordance at all (only Restore) — it's render-only, same as
  `NoteArchivedException`'s existing "restore before editing" invariant, now
  enforced by the UI itself rather than just a disabled/readonly textarea.
  New state: `IsEditingContent` (+ `ShowEditor => IsCreating ||
  IsEditingContent`); `OpenNoteAsync`/`StartNewNote` reset it, `StartEditing`
  sets it. No backend/contract change — `Content` is still one Markdown
  string, this only changes which of the two already-existing render paths
  (raw textarea vs. `PreviewHtml`) is shown by default.
- **Addendum — copy button on code blocks.** Each rendered `<pre>` in the
  preview/view gets a small hover-revealed "Copy" button (`notes-editor.js`,
  added alongside the Prism highlighting pass since both need redoing every
  render), using `navigator.clipboard.writeText`. Pure DOM/JS, no Blazor
  component per code block — keeps the "one Markdown string, no block
  model" decision above intact instead of needing per-block component
  interactivity.

Out of scope for this iteration (see section 3a for the full reasoning):

- **Nested/hierarchical notes** (Notion sub-pages / Confluence page tree).
  Needs a self-referencing `ParentNoteId`, cycle prevention, and a tree UI
  widget, for a benefit tags + wiki-links already cover reasonably well for
  one student's notes. Worth its own follow-up plan if it turns out to be
  needed.
- **Graph view** of note links (Obsidian's node graph). The backlinks panel
  surfaces the same underlying data without a force-directed graph
  renderer; revisit only if flat backlink lists prove insufficient.
- **Comments / mentions** (Confluence's/Notion's collaboration surface).
  StudyHub is explicitly a single-user personal tool (`CLAUDE.md`: "a
  personal learning companion") — there's no second user to comment as.
- AI summarization / flashcard generation from notes.
- Rich-text/WYSIWYG or a **real block-based document model** (Notion's
  block editor: each block a separate persisted node, drag-drop reordering,
  contenteditable), image embedding, or drag-drop file upload directly
  inline in the Markdown body — plain Markdown already covers
  headings/lists/quotes/code a block editor would also offer, at a fraction
  of the UI cost. The slash-command menu (addendum above) gets the
  Confluence/Notion *feel* — typing `/` to insert an element — without this:
  it's a snippet inserter over one plain-text `Content` field, not a
  per-block schema.
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
- **Note titles must be unique**, mirroring `Course.Name`/`Semester.Name`'s
  existing unique index. This is what makes `[[Title]]` wiki-link resolution
  deterministic without needing a disambiguation picker. Enforced via a
  unique index + a `DuplicateNoteTitleException` (same shape as
  `DuplicateCourseNameException`/`DuplicateSemesterNameException`).
- **Wiki-link resolution rule:** exact, case-insensitive title match,
  re-parsed from `Content` and re-resolved into the `NoteLinks` table on
  every create/update. A `[[Title]]` that doesn't match any existing note
  renders as plain (unlinked) text in the preview — there's no Obsidian-style
  "create note from broken link" flow in this iteration (nothing stops it
  being added later; it doesn't affect the schema).

## 3a. Notion / Confluence / Obsidian feature survey

Requested: "grundlegende Features wie in Notion oder Confluence" / a
lightweight Obsidian-like note system. Notion, Confluence, and Obsidian are
general-purpose, multi-user or plugin-driven products, so instead of
adopting any of them wholesale, each commonly-associated feature was
evaluated against `CLAUDE.md`'s "build the smallest useful solution" /
"avoid over-engineering" principle and against StudyHub's actual context
(one student, Course/Semester-scoped notes, no collaboration surface):

| Feature | Source | Decision | Why |
|---|---|---|---|
| `[[Wiki-links]]` between notes | Obsidian | **In scope** | The one feature that's genuinely hard to bolt on later — link storage needs a real table from day one. Cheap otherwise: one join table (`NoteLinks`), parsed from `Content` on save. |
| Backlinks panel | Obsidian | **In scope** | Direct payoff of wiki-links — a single indexed query (`NoteLinks` where `TargetNoteId = @id`), no extra modeling. |
| Heading outline / mini-TOC | Notion, Confluence | **In scope** | Free once Markdig parses `Content` for the preview anyway; no schema impact. |
| Note templates (starter snippets) | Notion | **In scope** | Pure UI convenience, no schema impact, trivial to extend later. |
| Tags + tag filter | Notion, Confluence, Obsidian | **Already in scope** | Carried over from the original plan (section 3). |
| Nested pages / page tree | Notion, Confluence | **Deferred** | Needs `ParentNoteId` + cycle prevention + a tree UI component; tags + wiki-links already give a lightweight organizing structure. Candidate for a dedicated follow-up plan. |
| Graph view | Obsidian | **Deferred** | A force-directed graph is a sizeable standalone UI component; the backlinks panel exposes the same underlying data first, more cheaply. |
| Comments / mentions | Confluence, Notion | **Out of scope** | No second user in a personal, single-user tool — nobody to comment as. |
| Real-time collaborative editing | Notion, Confluence | **Out of scope** | Same reason; also already out of scope in the original plan. |
| Block-based editing (drag-and-drop blocks) | Notion | **Out of scope** | Would mean replacing the plain Markdown textarea with a block editor — a large UI investment; Markdown already covers the headings/lists/quotes/code a block editor would also offer. |

If nested pages or a graph view turn out to matter in practice, both are
additive on top of a flat `Notes` table + `NoteLinks` — neither needs to
change to add either one later.

## 4. Architecture Impact

Mirrors the existing `Documents`/`Courses`/`Semesters` features exactly
(contracts-first: `UI` → `Business` contract → `Domain` contract →
`Infrastructure` implementation):

| Layer | Project | Additions |
|---|---|---|
| Domain | `StudyHub.Logic.Domain` | `Notes/Note.cs` (entity: `Title`, `Content`, `Tags`, `CourseId`/`SemesterId`, `IsArchived`, `Archive()`/`Restore()`, `Update()`), `INoteRepository` (CRUD + `GetAttachedDocumentIdsAsync`/`AddAttachmentAsync`/`RemoveAttachmentAsync` + `ReplaceLinksAsync`/`GetLinkedNoteIdsAsync`/`GetBacklinkNoteIdsAsync` for `NoteLinks`), `NoteNotFoundException`, `NoteValidationException`, `NoteArchivedException`, `DuplicateNoteTitleException` |
| Business | `StudyHub.Logic.Business` | `Notes/INoteManagement.cs`, `NoteManagement.cs` (CRUD, `ArchiveAsync`/`RestoreAsync`, `AttachDocumentAsync`/`DetachDocumentAsync`, wiki-link extraction on create/update — parses `[[Title]]` out of `Content` and calls `INoteRepository.ReplaceLinksAsync`, `GetBacklinksAsync` — depends on `INoteRepository` + `IDocumentRepository` + `ICourseRepository` + `ISemesterRepository`, same "Business depends on Domain repository contracts directly" pattern `DocumentManagement` already uses), `NoteDto` (incl. `AttachedDocumentIds`, `LinkedNoteIds`), `NoteBacklinkDto`, `CreateNoteRequest`, `UpdateNoteRequest`, `NoteErrorCodes` |
| Infrastructure | `StudyHub.Infrastructure` | `Notes/NoteRepository.cs` (EF Core repository incl. `NoteDocuments` and `NoteLinks` join queries) |
| Data | `StudyHub.Data` | `DbSet<Note>` + `DbSet<NoteDocument>` + `DbSet<NoteLink>`, `OnModelCreating` configuration (mirrors `Document`'s check-constraint pattern), one EF Core migration |
| UI (API) | `StudyHub.Api` | `Notes/NoteEndpoints.cs` (list/by-course/by-semester/by-id, create, update, archive, restore, attach/detach document, `{id}/backlinks`), `NoteExceptionHandler.cs` |
| UI (Blazor) | `StudyHub.UI` | `Notes/NoteApiClient.cs`, rewritten `Components/Pages/Notes.razor` (+ code-behind) with note list + Markdown editor/preview + heading outline + backlinks panel + template picker, `Components/Shared/NoteAttachmentPicker.razor` (reuses `DocumentUploadDialog` + existing `DocumentDto` list) |
| Tests | `StudyHub.Tests` | `Logic/Domain/Notes/NoteTests.cs`, `Logic/Business/Notes/NoteManagementTests.cs` (incl. wiki-link parsing/resolution cases), `Infrastructure/Notes/NoteRepositoryTests.cs`, `Api/Notes/NoteEndpointsTests.cs` |

DI wiring (mirrors `Document` exactly):

- `StudyHub.Infrastructure/ServiceCollectionExtensions.cs`: register `INoteRepository → NoteRepository`.
- `StudyHub.Logic.Business/ServiceCollectionExtensions.cs`: register `INoteManagement → NoteManagement`.
- `StudyHub.UI/Program.cs`: `builder.Services.AddHttpClient<INoteManagement, NoteApiClient>(...)`.

## 5. Database Impact

- New table `Notes`: `Id`, `Title` (unique index, see section 3), `Content`
  (capped at 50,000 chars), `Tags` (delimited string, nullable), `CourseId`
  (nullable FK, `Restrict`), `SemesterId` (nullable FK, `Restrict`),
  `IsArchived`, `CreatedAt`, `UpdatedAt`. Check constraint enforcing exactly
  one of `CourseId`/`SemesterId`, same as `Documents.CK_Documents_ExactlyOneParent`.
- New join table `NoteDocuments`: `NoteId` (FK → `Notes`, `Restrict`),
  `DocumentId` (FK → `Documents`, `Restrict`), composite primary key
  `(NoteId, DocumentId)`. `Restrict` on both sides for the same reason as
  elsewhere in the schema — notes and documents are only ever archived, never
  hard-deleted, so a physical delete must not silently cascade.
- New join table `NoteLinks` (wiki-links, section 3a): `SourceNoteId` (FK →
  `Notes`, `Restrict`), `TargetNoteId` (FK → `Notes`, `Restrict`), composite
  primary key `(SourceNoteId, TargetNoteId)`. Repopulated wholesale for a
  note (delete-then-reinsert its outgoing rows) every time `NoteManagement`
  parses `[[Title]]` links out of `Content` on create/update — same
  "recompute on save" approach as `Course`/`Semester`/`Document`'s
  archive/restore, just applied to a derived join instead of a flag.
- Indexes on `Notes.CourseId`, `Notes.SemesterId`, `NoteDocuments.DocumentId`,
  and `NoteLinks.TargetNoteId` (the backlinks query).
- One EF Core migration ("AddNote"), per `CLAUDE.md`'s "one migration per
  feature" — `Notes`, `NoteDocuments`, and `NoteLinks` all land in that same
  migration since they're one feature. No provider change — stays on the
  existing SQLite database.

## 6. Task Checklist

### Documentation (first, per section 0)

- [ ] Rewrite `docs/architecture.md` to describe StudyHub's actual layers/
      components instead of the unrelated Polipol.AppLauncher/CoCo content.
- [ ] Rewrite `docs/agent-context.md` to describe StudyHub's actual repo map,
      domain glossary, and conventions instead of AppLauncher's.
- [ ] Remove the dangling `docs/roadmap.md`/`docs/adrs/` references from
      `CLAUDE.md`'s "Documentation" section.

### Backend

- [ ] `Note` domain entity + `INoteRepository` + EF configuration/migration
      (incl. `NoteDocuments` and `NoteLinks` join tables).
- [ ] `NoteManagement` (create/update/archive/restore, attach/detach document,
      wiki-link parsing + `NoteLinks` resolution on save, `GetBacklinksAsync`,
      validation: title required + unique, content size cap, exactly-one-parent,
      parent existence/not-archived — mirrors `DocumentManagement`).
- [ ] `NoteEndpoints` (list/by-course/by-semester/by-id, create, update,
      archive, restore, attach/detach document, `{id}/backlinks`) +
      `NoteExceptionHandler`.

### UI

- [ ] `NoteApiClient` implementing `INoteManagement` over HTTP (mirrors
      `DocumentApiClient`, including `ProblemDetails` → exception mapping
      for `NoteErrorCodes` and the reused `Document`/`Course`/`Semester`
      error codes).
- [ ] `Notes.razor` rewrite: note list (search box, tag filter chips, scope
      filter reusing the `Documents.razor` course/semester `<select>`
      pattern) + Markdown editor pane with live preview (Markdig).
- [ ] "New note" (with template picker) / "attach document" flows per
      section 3.
- [ ] Wiki-link click-to-navigate in the preview pane + "Linked mentions"
      (backlinks) panel + heading outline mini-TOC.
- [ ] Archive/restore actions (mirrors `DocumentDetailsDialog`'s pattern).

## 7. Validation Plan

- `dotnet build`
- `dotnet test`
- Manual verification in the running Blazor UI: create a note under a
  Course, create one under a Semester, edit content and confirm the
  Markdown preview updates, attach an existing document, upload+attach a
  new document, archive/restore a note, filter by tag and by course/semester
  scope, link two notes with `[[Title]]` and confirm the backlink shows up
  on the target note, confirm a `[[Missing Title]]` link renders unlinked.

## 8. Risks / Assumptions

- All items under section 3 ("Decisions") are explicit assumptions —
  particularly the new Markdig dependency, the unrestricted
  note-to-document attachment scope, and the new unique-title constraint —
  and should be confirmed before implementation starts.
- 50,000 characters and the tag-as-string approach are the two most likely
  candidates to need revisiting if usage patterns turn out different than
  expected (e.g. long lecture-note transcripts, or a desire to rename a tag
  across all notes at once).
- **Unique titles** is a new constraint not present on any other entity
  except by convention (`Course`/`Semester` also enforce it) — renaming a
  note to an existing title now needs a clear validation error in the UI,
  not just a 500 from a DB unique-index violation.
- Recomputing `NoteLinks` on every save is O(number of `[[links]]` in the
  note), not O(all notes) — fine at personal-notes scale, called out in case
  note counts ever grow far beyond what one student would produce.
- Section 3a's "deferred" items (nested pages, graph view) are deliberately
  not designed for here beyond "additive later" — if either is wanted for
  this iteration after all, say so and this plan gets revised before
  implementation starts.
