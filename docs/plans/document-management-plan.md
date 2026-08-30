# Feature Plan: Document Management

Status: Draft
Classification (per `CLAUDE.md`): **Large** (File management) — implementation requires an
approved plan and explicit confirmation before any code is written.

## 1. Goal

Allow a student to upload, store, list, view details of, and download documents
(course materials, scripts, own notes as files) attached to a Course.

## 2. Scope

In scope:

- Upload of a document file, associated with a `Course`.
- Persisting the file content and its metadata.
- Listing documents (per course).
- Viewing document details (metadata).
- Downloading a previously uploaded document.

Out of scope (future milestones per `docs/roadmap.md`):

- OCR / handwritten note import.
- Full-text search across documents.
- Chat-with-documents (RAG).
- Versioning of documents.

## 3. Decisions and Open Questions

### Decided

- **File content storage: `byte[]` BLOB column in SQLite** (the project's
  existing provider, confirmed in `StudyHub.Api/ServiceCollectionExtensions.cs`
  via `UseSqlite`). No new database/storage service is introduced.
  Rationale: single-file, local-first persistence fits the project's scope
  (personal, single-user tool); SQLite's own benchmarks show BLOBs up to
  roughly the low-tens-of-MB range perform comparably to or better than
  filesystem storage. EF Core reads the whole BLOB into memory per
  request — acceptable for typical course materials (scripts, PDFs, notes),
  not intended for very large files (e.g. lecture video recordings).
  Consequence: enforce a hard upload size cap (see below) rather than
  building a separate storage abstraction now. If the project later needs
  filesystem/blob storage (e.g. for cloud sync, multi-user), only
  `IDocumentRepository`'s implementation changes — the `Domain`/`Business`
  contracts stay the same.

### Still open (require answers before implementation starts)

- Allowed file types / max file size? (Proposed default: cap at 25 MB per
  file, no type allow-list beyond a basic block-list — confirm.)
- Is a document always attached to exactly one `Course`, or can it also stand
  alone (e.g. attached to a `Semester`, or unattached)?
- Do we need soft-delete/archive semantics like `Course.IsArchived`, or a hard
  delete?

These remaining decisions affect the domain model and database schema and
should be settled before Domain work starts.

## 4. Architecture Impact

Following the project's Composite Component layering
(`Shared` / `Data` / `Logic{Domain, Business, Integration}` / `Infrastructure` / `UI`),
mirrored on the existing `Courses` and `Semesters` features:

| Layer | Project | Additions |
|---|---|---|
| Domain | `StudyHub.Logic.Domain` | `Documents/Document.cs` (entity), `IDocumentRepository`, domain exceptions (e.g. `DocumentNotFoundException`, `DocumentValidationException`) |
| Business | `StudyHub.Logic.Business` | `Documents/IDocumentManagement.cs`, `DocumentManagement.cs`, `DocumentDto`, `UploadDocumentRequest`, `DocumentErrorCodes` |
| Infrastructure | `StudyHub.Infrastructure` | `Documents/DocumentRepository.cs` (EF Core repository; file content is persisted as a `byte[]` column, no separate storage adapter needed) |
| Data | `StudyHub.Data` | `DbSet<Document>` + `IEntityTypeConfiguration<Document>` (or inline `OnModelCreating` per current convention), one EF Core migration |
| UI (API) | `StudyHub.Api` | `Documents/DocumentEndpoints.cs` (upload, list, get by id, download), `DocumentExceptionHandler.cs` |
| UI (Blazor) | `StudyHub.UI` | `Documents/DocumentApiClient.cs`, `Components/Pages/Documents.razor` (+ code-behind), upload dialog component, document details component |
| Tests | `StudyHub.Tests` | Business-logic tests for `DocumentManagement` (upload validation, not-found, mapping) |

Contracts-first: `UI` calls only `IDocumentManagement` (Business); `Business`
calls only `IDocumentRepository` (Domain); `Infrastructure` implements both
the repository and the storage adapter behind their interfaces.

## 5. Database Impact

- New table `Documents` (one EF Core migration, per `CLAUDE.md` "one migration
  per feature").
- Columns: `Id`, `CourseId` (FK, `Restrict` delete like `Course` → `Semester`),
  `FileName`, `ContentType`, `SizeBytes`, `Content` (`byte[]`/BLOB),
  `CreatedAt`, `UpdatedAt` (pending §3's remaining open questions on
  file-type/size limits and archive semantics).
- Index on `CourseId` for listing by course.
- No schema/provider change needed — stays on the existing SQLite database.

## 6. Task Checklist

### Backend

- [ ] File Upload — API endpoint accepting a document file + metadata,
      validated in `DocumentManagement` (size/type limits, course existence).
- [ ] Speicherung (Storage) — `Document` domain entity, `IDocumentRepository`
      + EF configuration/migration; file content stored as a `byte[]` BLOB
      column in the existing SQLite database (no new storage service).
- [ ] Download — API endpoint streaming a stored document back by id.

### UI

- [ ] Dokumentliste (Document list) — Blazor page/component listing documents
      for a course, using `DocumentApiClient`.
- [ ] Upload Dialog — modal/component for selecting and uploading a file.
- [ ] Dokumentdetails (Document details) — component showing metadata and a
      download action for a single document.

## 7. Validation Plan

- `dotnet build`
- `dotnet test`
- Manual verification of upload → list → details → download round-trip in
  the running Blazor UI.

## 8. Risks / Assumptions

- This plan assumes documents attach to a `Course` (mirroring the existing
  `Courses`/`Semesters` FK pattern); confirm before implementing if a
  different association is intended.
- Storing content as BLOBs in SQLite means the `.db` file grows with every
  upload; an upload size cap (proposed 25 MB, §3) is the main safeguard
  against unbounded growth. No streaming download — content is loaded fully
  into memory per request, acceptable for typical document sizes but not for
  very large files.
- Remaining open questions in §3 (size/type limits, association, archive vs.
  hard delete) should be confirmed before Domain work starts.
