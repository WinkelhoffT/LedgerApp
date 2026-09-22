using Microsoft.EntityFrameworkCore;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Documents;
using StudyHub.Shared.Domain.Courses;
using StudyHub.Shared.Domain.Documents;
using StudyHub.Shared.Domain.Notes;
using StudyHub.Shared.Domain.Semesters;
using StudyHub.Shared.Notes;
using StudyHub.Shared.Semesters;

namespace StudyHub.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();

    public DbSet<Semester> Semesters => Set<Semester>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<Note> Notes => Set<Note>();

    public DbSet<NoteDocument> NoteDocuments => Set<NoteDocument>();

    public DbSet<NoteLink> NoteLinks => Set<NoteLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(builder =>
        {
            builder.ToTable("Courses");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .HasMaxLength(CreateCourseRequest.NameMaxLength)
                .IsRequired();

            builder.Property(c => c.Description)
                .HasMaxLength(CreateCourseRequest.DescriptionMaxLength);

            builder.Property(c => c.Color)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(c => c.SemesterId)
                .IsRequired();

            builder.Property(c => c.IsArchived)
                .IsRequired();

            builder.Property(c => c.CreatedAt)
                .IsRequired();

            builder.Property(c => c.UpdatedAt)
                .IsRequired();

            builder.HasIndex(c => c.Name)
                .IsUnique();

            builder.HasIndex(c => c.SemesterId);

            // Restrict, not Cascade: neither entity is ever hard-deleted (only archived),
            // so a physical delete of a Semester should never silently take its Courses with it.
            builder.HasOne<Semester>()
                .WithMany()
                .HasForeignKey(c => c.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Semester>(builder =>
        {
            builder.ToTable("Semesters");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name)
                .HasMaxLength(CreateSemesterRequest.NameMaxLength)
                .IsRequired();

            builder.Property(s => s.StartDate)
                .IsRequired();

            builder.Property(s => s.EndDate)
                .IsRequired();

            builder.Property(s => s.IsArchived)
                .IsRequired();

            builder.Property(s => s.CreatedAt)
                .IsRequired();

            builder.Property(s => s.UpdatedAt)
                .IsRequired();

            builder.HasIndex(s => s.Name)
                .IsUnique();
        });

        modelBuilder.Entity<Document>(builder =>
        {
            builder.ToTable("Documents");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.FileName)
                .HasMaxLength(UploadDocumentRequest.FileNameMaxLength)
                .IsRequired();

            builder.Property(d => d.ContentType)
                .HasMaxLength(UploadDocumentRequest.ContentTypeMaxLength)
                .IsRequired();

            builder.Property(d => d.SizeBytes)
                .IsRequired();

            builder.Property(d => d.Content)
                .IsRequired();

            builder.Property(d => d.IsArchived)
                .IsRequired();

            builder.Property(d => d.CreatedAt)
                .IsRequired();

            builder.Property(d => d.UpdatedAt)
                .IsRequired();

            builder.HasIndex(d => d.CourseId);

            builder.HasIndex(d => d.SemesterId);

            // Restrict, not Cascade: neither parent is ever hard-deleted (only archived), so a
            // physical delete of a Course/Semester should never silently take its Documents with it.
            builder.HasOne<Course>()
                .WithMany()
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Semester>()
                .WithMany()
                .HasForeignKey(d => d.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mirrors the invariant enforced in the Document domain constructor: a document
            // belongs to exactly one of a Course or a Semester, never both, never neither.
            builder.ToTable(t => t.HasCheckConstraint(
                "CK_Documents_ExactlyOneParent",
                "((\"CourseId\" IS NOT NULL AND \"SemesterId\" IS NULL) OR (\"CourseId\" IS NULL AND \"SemesterId\" IS NOT NULL))"));
        });

        modelBuilder.Entity<Note>(builder =>
        {
            builder.ToTable("Notes");

            builder.HasKey(n => n.Id);

            // NOCASE collation makes the unique index below enforce case-insensitive uniqueness
            // at the database level too, not just in NoteRepository.ExistsByTitleAsync — closing
            // the race window where two near-simultaneous creates with differently-cased titles
            // ("Foo" / "foo") could otherwise both pass the app-level check and be inserted.
            builder.Property(n => n.Title)
                .HasMaxLength(CreateNoteRequest.TitleMaxLength)
                .UseCollation("NOCASE")
                .IsRequired();

            builder.Property(n => n.Content)
                .HasMaxLength(CreateNoteRequest.ContentMaxLength)
                .IsRequired();

            builder.Property(n => n.Tags)
                .HasMaxLength(CreateNoteRequest.TagsMaxLength);

            builder.Property(n => n.IsArchived)
                .IsRequired();

            builder.Property(n => n.CreatedAt)
                .IsRequired();

            builder.Property(n => n.UpdatedAt)
                .IsRequired();

            builder.HasIndex(n => n.Title)
                .IsUnique();

            builder.HasIndex(n => n.CourseId);

            builder.HasIndex(n => n.SemesterId);

            // Restrict, not Cascade: neither parent is ever hard-deleted (only archived), so a
            // physical delete of a Course/Semester should never silently take its Notes with it.
            builder.HasOne<Course>()
                .WithMany()
                .HasForeignKey(n => n.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Semester>()
                .WithMany()
                .HasForeignKey(n => n.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mirrors the invariant enforced in the Note domain constructor: a note belongs to
            // exactly one of a Course or a Semester, never both, never neither.
            builder.ToTable(t => t.HasCheckConstraint(
                "CK_Notes_ExactlyOneParent",
                "((\"CourseId\" IS NOT NULL AND \"SemesterId\" IS NULL) OR (\"CourseId\" IS NULL AND \"SemesterId\" IS NOT NULL))"));
        });

        modelBuilder.Entity<NoteDocument>(builder =>
        {
            builder.ToTable("NoteDocuments");

            builder.HasKey(nd => new { nd.NoteId, nd.DocumentId });

            builder.HasIndex(nd => nd.DocumentId);

            // Restrict on both sides for the same reason as elsewhere in the schema — notes and
            // documents are only ever archived, never hard-deleted, so a physical delete must
            // never silently cascade.
            builder.HasOne<Note>()
                .WithMany()
                .HasForeignKey(nd => nd.NoteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Document>()
                .WithMany()
                .HasForeignKey(nd => nd.DocumentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NoteLink>(builder =>
        {
            builder.ToTable("NoteLinks");

            builder.HasKey(l => new { l.SourceNoteId, l.TargetNoteId });

            builder.HasIndex(l => l.TargetNoteId);

            // Restrict on both sides: links are recomputed on save (delete-then-reinsert), never
            // relied upon to cascade-delete a Note, which is itself only ever archived.
            builder.HasOne<Note>()
                .WithMany()
                .HasForeignKey(l => l.SourceNoteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Note>()
                .WithMany()
                .HasForeignKey(l => l.TargetNoteId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
