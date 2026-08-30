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

## 3. Open Questions (require answers before implementation starts)

- Where should file *content* live: on disk (path referenced from DB), in the
  database (`byte[]`/`varbinary`), or in blob storage? The existing `Course`/
  `Semester` features only persist metadata — there is no established
  convention yet for binary content.
- Allowed file types / max file size?
- Is a document always attached to exactly one `Course`, or can it also stand
  alone (e.g. attached to a `Semester`, or unattached)?
- Do we need soft-delete/archive semantics like `Course.IsArchived`, or a hard
  delete?

These decisions affect the domain model, the `Infrastructure` storage
component, and the database schema, so they should be settled before Domain
work starts.

## 4. Architecture Impact

Following the project's Composite Component layering
(`Shared` / `Data` / `Logic{Domain, Business, Integration}` / `Infrastructure` / `UI`),
mirrored on the existing `Courses` and `Semesters` features:

| Layer | Project | Additions |
|---|---|---|
| Domain | `StudyHub.Logic.Domain` | `Documents/Document.cs` (entity), `IDocumentRepository`, domain exceptions (e.g. `DocumentNotFoundException`, `DocumentValidationException`) |
| Business | `StudyHub.Logic.Business` | `Documents/IDocumentManagement.cs`, `DocumentManagement.cs`, `DocumentDto`, `UploadDocumentRequest`, `DocumentErrorCodes` |
| Infrastructure | `StudyHub.Infrastructure` | `Documents/DocumentRepository.cs`, file storage adapter (implementation depends on open question in §3) |
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
- Expected columns (pending §3 decisions): `Id`, `CourseId` (FK, `Restrict`
  delete like `Course` → `Semester`), `FileName`, `ContentType`, `SizeBytes`,
  `StoragePath` or `Content`, `CreatedAt`, `UpdatedAt`.
- Index on `CourseId` for listing by course.

## 6. Task Checklist

### Backend

- [ ] File Upload — API endpoint accepting a document file + metadata,
      validated in `DocumentManagement` (size/type limits, course existence).
- [ ] Speicherung (Storage) — `Document` domain entity, `IDocumentRepository`
      + EF configuration/migration, storage adapter for the file content.
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
- Storage mechanism is not yet decided (§3) — implementation should not start
  on Storage/Download until this is confirmed, since it changes the
  `Infrastructure` and `Data` layer shape significantly.
