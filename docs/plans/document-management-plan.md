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

## 3. Decisions

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

- **Allowed file types: PDF and DOCX only** (`application/pdf`,
  `application/vnd.openxmlformats-officedocument.wordprocessingml.document`).
  Upload is rejected for any other content type/extension. Max file size:
  **25 MB per file** (assumption — flag if a different limit is needed).
- **Association: a document belongs to either a `Course` or a `Semester`,
  never both, never neither.** `Document` gets two nullable FKs (`CourseId`,
  `SemesterId`) with an invariant — enforced both in the domain constructor
  and as a DB check constraint — that exactly one is set.
- **Soft delete**, mirroring the existing `Course`/`Semester` pattern: a
  `Document.IsArchived` flag with `Archive()`/`Restore()` domain methods and
  a `DocumentArchivedException` (same shape as `CourseArchivedException`).
  `GetAllAsync`/list queries return archived documents too (as with
  `Course`/`Semester` today) — filtering archived vs. active is a UI/Business
  concern, not baked into the repository.

## 4. Architecture Impact

Following the project's Composite Component layering
(`Shared` / `Data` / `Logic{Domain, Business, Integration}` / `Infrastructure` / `UI`),
mirrored on the existing `Courses` and `Semesters` features:

| Layer | Project | Additions |
|---|---|---|
| Domain | `StudyHub.Logic.Domain` | `Documents/Document.cs` (entity with `Archive()`/`Restore()`), `IDocumentRepository`, domain exceptions (`DocumentNotFoundException`, `DocumentValidationException`, `DocumentArchivedException`) |
| Business | `StudyHub.Logic.Business` | `Documents/IDocumentManagement.cs`, `DocumentManagement.cs` (incl. `ArchiveAsync`/`RestoreAsync`, content-type/size validation), `DocumentDto`, `UploadDocumentRequest`, `DocumentErrorCodes` |
| Infrastructure | `StudyHub.Infrastructure` | `Documents/DocumentRepository.cs` (EF Core repository; file content is persisted as a `byte[]` column, no separate storage adapter needed) |
| Data | `StudyHub.Data` | `DbSet<Document>` + `IEntityTypeConfiguration<Document>` (or inline `OnModelCreating` per current convention), one EF Core migration |
| UI (API) | `StudyHub.Api` | `Documents/DocumentEndpoints.cs` (upload, list, get by id, download), `DocumentExceptionHandler.cs` |
| UI (Blazor) | `StudyHub.UI` | `Documents/DocumentApiClient.cs`, `Components/Pages/Documents.razor` (+ code-behind), upload dialog component, document details component |
| Tests | `StudyHub.Tests` | Business-logic tests for `DocumentManagement` (upload validation, not-found, mapping) |

Contracts-first: `UI` calls only `IDocumentManagement` (Business); `Business`
calls only `IDocumentRepository` (Domain); `Infrastructure` implements the
repository (content lives in the same table, no separate storage adapter).

## 5. Database Impact

- New table `Documents` (one EF Core migration, per `CLAUDE.md` "one migration
  per feature").
- Columns: `Id`, `CourseId` (nullable FK), `SemesterId` (nullable FK, both
  `Restrict` delete — same reasoning as `Course` → `Semester` today: neither
  parent is hard-deleted, only archived), `FileName`, `ContentType`
  (restricted to PDF/DOCX at the Business layer), `SizeBytes`, `Content`
  (`byte[]`/BLOB, capped at 25 MB), `IsArchived`, `CreatedAt`, `UpdatedAt`.
- Check constraint enforcing exactly one of `CourseId`/`SemesterId` is set
  (`HasCheckConstraint` in `OnModelCreating`, mirroring the mutual-exclusivity
  invariant already enforced in the `Document` domain constructor).
- Indexes on `CourseId` and `SemesterId` for listing by parent.
- No schema/provider change needed — stays on the existing SQLite database.

## 6. Task Checklist

### Backend

- [ ] File Upload — API endpoint accepting a document file + metadata
      (either a `CourseId` or a `SemesterId`), validated in
      `DocumentManagement` (PDF/DOCX only, ≤25 MB, parent existence,
      exactly-one-parent invariant).
- [ ] Speicherung (Storage) — `Document` domain entity (mutually-exclusive
      `CourseId`/`SemesterId`, `IsArchived` + `Archive()`/`Restore()`),
      `IDocumentRepository` + EF configuration/migration; file content
      stored as a `byte[]` BLOB column in the existing SQLite database
      (no new storage service).
- [ ] Download — API endpoint streaming a stored document back by id.

### UI

- [ ] Dokumentliste (Document list) — Blazor page/component listing documents
      for a course or a semester, using `DocumentApiClient`.
- [ ] Upload Dialog — modal/component for selecting and uploading a file.
- [ ] Dokumentdetails (Document details) — component showing metadata and a
      download action for a single document.

## 7. Validation Plan

- `dotnet build`
- `dotnet test`
- Manual verification of upload → list → details → download round-trip in
  the running Blazor UI.

## 8. Risks / Assumptions

- Storing content as BLOBs in SQLite means the `.db` file grows with every
  upload; the 25 MB upload cap is the main safeguard against unbounded
  growth. No streaming download — content is loaded fully into memory per
  request, acceptable for typical document sizes but not for very large
  files.
- The exactly-one-of-`CourseId`/`SemesterId` invariant needs enforcement at
  two levels (domain constructor + DB check constraint) since nothing in the
  existing codebase establishes a precedent for this kind of "belongs to one
  of several parents" relationship — reviewed closely during implementation.
- 25 MB is an assumed size cap, not one the user explicitly confirmed;
  flag if a different limit is needed.
