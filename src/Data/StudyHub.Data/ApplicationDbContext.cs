using Microsoft.EntityFrameworkCore;
using StudyHub.Shared.CalendarEvents;
using StudyHub.Shared.Courses;
using StudyHub.Shared.Documents;
using StudyHub.Shared.Flashcards;
using StudyHub.Shared.Notes;
using StudyHub.Shared.Semesters;
using StudyHub.Shared.StudySessions;

namespace StudyHub.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();

    public DbSet<Semester> Semesters => Set<Semester>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<Note> Notes => Set<Note>();

    public DbSet<NoteDocument> NoteDocuments => Set<NoteDocument>();

    public DbSet<NoteLink> NoteLinks => Set<NoteLink>();

    public DbSet<FlashcardDeck> FlashcardDecks => Set<FlashcardDeck>();

    public DbSet<Flashcard> Flashcards => Set<Flashcard>();

    public DbSet<FlashcardReview> FlashcardReviews => Set<FlashcardReview>();

    public DbSet<StudySession> StudySessions => Set<StudySession>();

    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(builder =>
        {
            builder.ToTable("Courses");

            builder.HasKey(c => c.Id);

            builder
                .Property(c => c.Name)
                .HasMaxLength(CreateCourseRequest.NameMaxLength)
                .IsRequired();

            builder
                .Property(c => c.Description)
                .HasMaxLength(CreateCourseRequest.DescriptionMaxLength);

            builder.Property(c => c.Color).HasMaxLength(20).IsRequired();

            builder.Property(c => c.SemesterId).IsRequired();

            builder.Property(c => c.IsArchived).IsRequired();

            builder.Property(c => c.CreatedAt).IsRequired();

            builder.Property(c => c.UpdatedAt).IsRequired();

            builder.HasIndex(c => c.Name).IsUnique();

            builder.HasIndex(c => c.SemesterId);

            // Restrict, not Cascade: neither entity is ever hard-deleted (only archived),
            // so a physical delete of a Semester should never silently take its Courses with it.
            builder
                .HasOne<Semester>()
                .WithMany()
                .HasForeignKey(c => c.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Semester>(builder =>
        {
            builder.ToTable("Semesters");

            builder.HasKey(s => s.Id);

            builder
                .Property(s => s.Name)
                .HasMaxLength(CreateSemesterRequest.NameMaxLength)
                .IsRequired();

            builder.Property(s => s.StartDate).IsRequired();

            builder.Property(s => s.EndDate).IsRequired();

            builder.Property(s => s.IsArchived).IsRequired();

            builder.Property(s => s.CreatedAt).IsRequired();

            builder.Property(s => s.UpdatedAt).IsRequired();

            builder.HasIndex(s => s.Name).IsUnique();
        });

        modelBuilder.Entity<Document>(builder =>
        {
            builder.ToTable("Documents");

            builder.HasKey(d => d.Id);

            builder
                .Property(d => d.FileName)
                .HasMaxLength(UploadDocumentRequest.FileNameMaxLength)
                .IsRequired();

            builder
                .Property(d => d.ContentType)
                .HasMaxLength(UploadDocumentRequest.ContentTypeMaxLength)
                .IsRequired();

            builder.Property(d => d.SizeBytes).IsRequired();

            builder.Property(d => d.Content).IsRequired();

            builder.Property(d => d.IsArchived).IsRequired();

            builder.Property(d => d.CreatedAt).IsRequired();

            builder.Property(d => d.UpdatedAt).IsRequired();

            builder.HasIndex(d => d.CourseId);

            builder.HasIndex(d => d.SemesterId);

            // Restrict, not Cascade: neither parent is ever hard-deleted (only archived), so a
            // physical delete of a Course/Semester should never silently take its Documents with it.
            builder
                .HasOne<Course>()
                .WithMany()
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne<Semester>()
                .WithMany()
                .HasForeignKey(d => d.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mirrors the invariant enforced in the Document domain constructor: a document
            // belongs to exactly one of a Course or a Semester, never both, never neither.
            builder.ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Documents_ExactlyOneParent",
                    "((\"CourseId\" IS NOT NULL AND \"SemesterId\" IS NULL) OR (\"CourseId\" IS NULL AND \"SemesterId\" IS NOT NULL))"
                )
            );
        });

        modelBuilder.Entity<Note>(builder =>
        {
            builder.ToTable("Notes");

            builder.HasKey(n => n.Id);

            builder.Property(n => n.Title).HasMaxLength(Note.TitleMaxLength).IsRequired();

            builder.Property(n => n.Content).HasMaxLength(Note.ContentMaxLength).IsRequired();

            builder.Property(n => n.Tags).HasMaxLength(Note.TagsMaxLength);

            builder.Property(n => n.IsArchived).IsRequired();

            builder.Property(n => n.CreatedAt).IsRequired();

            builder.Property(n => n.UpdatedAt).IsRequired();

            builder.HasIndex(n => n.Title).IsUnique();

            builder.HasIndex(n => n.CourseId);

            builder.HasIndex(n => n.SemesterId);

            // Restrict, not Cascade: neither parent is ever hard-deleted (only archived), so a
            // physical delete of a Course/Semester should never silently take its Notes with it.
            builder
                .HasOne<Course>()
                .WithMany()
                .HasForeignKey(n => n.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne<Semester>()
                .WithMany()
                .HasForeignKey(n => n.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mirrors the invariant enforced in the Note domain constructor: a note belongs to
            // exactly one of a Course or a Semester, never both, never neither.
            builder.ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Notes_ExactlyOneParent",
                    "((\"CourseId\" IS NOT NULL AND \"SemesterId\" IS NULL) OR (\"CourseId\" IS NULL AND \"SemesterId\" IS NOT NULL))"
                )
            );
        });

        modelBuilder.Entity<NoteDocument>(builder =>
        {
            builder.ToTable("NoteDocuments");

            builder.HasKey(nd => new { nd.NoteId, nd.DocumentId });

            builder.HasIndex(nd => nd.DocumentId);

            // Restrict on both sides for the same reason as elsewhere in the schema — notes and
            // documents are only ever archived, never hard-deleted, so a physical delete must
            // never silently cascade.
            builder
                .HasOne<Note>()
                .WithMany()
                .HasForeignKey(nd => nd.NoteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne<Document>()
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
            builder
                .HasOne<Note>()
                .WithMany()
                .HasForeignKey(l => l.SourceNoteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne<Note>()
                .WithMany()
                .HasForeignKey(l => l.TargetNoteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FlashcardDeck>(builder =>
        {
            builder.ToTable("FlashcardDecks");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Name).HasMaxLength(FlashcardDeck.NameMaxLength).IsRequired();

            builder.Property(d => d.NewCardsPerDay).IsRequired();

            builder.Property(d => d.ReviewsPerDay).IsRequired();

            builder.Property(d => d.IsArchived).IsRequired();

            builder.Property(d => d.CreatedAt).IsRequired();

            builder.Property(d => d.UpdatedAt).IsRequired();

            builder.HasIndex(d => d.Name).IsUnique();

            builder.HasIndex(d => d.CourseId);

            builder.HasIndex(d => d.SemesterId);

            // Restrict, not Cascade: courses and semesters are only ever archived, never hard-deleted.
            builder
                .HasOne<Course>()
                .WithMany()
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne<Semester>()
                .WithMany()
                .HasForeignKey(d => d.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mirrors the rule in FlashcardDeckLifecycle: a deck belongs to at most one of a Course
            // or a Semester (unlike notes and documents, it may also belong to neither).
            builder.ToTable(t =>
                t.HasCheckConstraint(
                    "CK_FlashcardDecks_AtMostOneParent",
                    "(\"CourseId\" IS NULL OR \"SemesterId\" IS NULL)"
                )
            );
        });

        modelBuilder.Entity<Flashcard>(builder =>
        {
            builder.ToTable("Flashcards");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Front).HasMaxLength(FlashcardDto.FrontMaxLength).IsRequired();

            builder.Property(c => c.Back).HasMaxLength(FlashcardDto.BackMaxLength).IsRequired();

            builder.Property(c => c.Tags).HasMaxLength(Flashcard.TagsMaxLength);

            builder.Property(c => c.State).IsRequired();

            builder.Property(c => c.DueAt).IsRequired();

            builder.Property(c => c.CreatedAt).IsRequired();

            builder.Property(c => c.UpdatedAt).IsRequired();

            // Serves the study queue: the next learning/review/new card of a deck, ordered by due time.
            builder.HasIndex(c => new
            {
                c.DeckId,
                c.State,
                c.DueAt,
            });

            // Restrict: decks are only ever archived, so deleting one must never take its cards with it.
            builder
                .HasOne<FlashcardDeck>()
                .WithMany()
                .HasForeignKey(c => c.DeckId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne<Note>()
                .WithMany()
                .HasForeignKey(c => c.SourceNoteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FlashcardReview>(builder =>
        {
            builder.ToTable("FlashcardReviews");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.ReviewedAt).IsRequired();

            builder.Property(r => r.Rating).IsRequired();

            builder.Property(r => r.StateBefore).IsRequired();

            builder.HasIndex(r => r.ReviewedAt);

            // Cascade on purpose: unlike the soft-deleted aggregates, a single card is deleted for
            // real (as in Anki), and its review log goes with it.
            builder
                .HasOne<Flashcard>()
                .WithMany()
                .HasForeignKey(r => r.FlashcardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StudySession>(builder =>
        {
            builder.ToTable("StudySessions");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Title).HasMaxLength(StudySession.TitleMaxLength).IsRequired();

            builder.Property(s => s.Date).IsRequired();

            builder.Property(s => s.StartTime).IsRequired();

            builder.Property(s => s.DurationMinutes).IsRequired();

            builder.Property(s => s.Location).HasMaxLength(StudySession.LocationMaxLength);

            builder.Property(s => s.CreatedAt).IsRequired();

            builder.Property(s => s.UpdatedAt).IsRequired();

            // Serves the month and week views, which load the sessions of a date range.
            builder.HasIndex(s => s.Date);

            builder.HasIndex(s => s.CourseId);

            builder.HasIndex(s => s.SemesterId);

            // Restrict, not Cascade: courses and semesters are only ever archived, never hard-deleted.
            builder
                .HasOne<Course>()
                .WithMany()
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne<Semester>()
                .WithMany()
                .HasForeignKey(s => s.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mirror the rules in StudySessionLifecycle; the same-day rule stays in the Domain only.
            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_StudySessions_AtMostOneParent",
                    "(\"CourseId\" IS NULL OR \"SemesterId\" IS NULL)"
                );
                t.HasCheckConstraint(
                    "CK_StudySessions_Duration",
                    $"(\"DurationMinutes\" >= {StudySession.MinDurationMinutes} AND \"DurationMinutes\" <= {StudySession.MaxDurationMinutes})"
                );
            });
        });

        modelBuilder.Entity<CalendarEvent>(builder =>
        {
            builder.ToTable("CalendarEvents");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Kind).IsRequired();

            builder.Property(e => e.Title).HasMaxLength(CalendarEvent.TitleMaxLength).IsRequired();

            builder.Property(e => e.Date).IsRequired();

            builder.Property(e => e.Location).HasMaxLength(CalendarEvent.LocationMaxLength);

            builder.Property(e => e.CreatedAt).IsRequired();

            builder.Property(e => e.UpdatedAt).IsRequired();

            // Serves the calendar views and the Dashboard's upcoming events.
            builder.HasIndex(e => e.Date);

            builder.HasIndex(e => e.CourseId);

            builder.HasIndex(e => e.SemesterId);

            // Restrict, not Cascade: courses and semesters are only ever archived, never hard-deleted.
            builder
                .HasOne<Course>()
                .WithMany()
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne<Semester>()
                .WithMany()
                .HasForeignKey(e => e.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mirror the rules in CalendarEventLifecycle that hold for every kind; the kind-specific
            // rules (a timed exam needs a duration, a deadline has none) stay in the Domain only.
            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_CalendarEvents_AtMostOneParent",
                    "(\"CourseId\" IS NULL OR \"SemesterId\" IS NULL)"
                );
                t.HasCheckConstraint(
                    "CK_CalendarEvents_Duration",
                    $"(\"DurationMinutes\" IS NULL OR (\"DurationMinutes\" >= {CalendarEvent.MinDurationMinutes} AND \"DurationMinutes\" <= {CalendarEvent.MaxDurationMinutes}))"
                );
                t.HasCheckConstraint(
                    "CK_CalendarEvents_DurationNeedsStart",
                    "(\"DurationMinutes\" IS NULL OR \"StartTime\" IS NOT NULL)"
                );
            });
        });
    }
}
