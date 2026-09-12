using Microsoft.EntityFrameworkCore;
using StudyHub.Logic.Domain.Courses;
using StudyHub.Logic.Domain.Documents;
using StudyHub.Logic.Domain.Semesters;

namespace StudyHub.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();

    public DbSet<Semester> Semesters => Set<Semester>();

    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(builder =>
        {
            builder.ToTable("Courses");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .HasMaxLength(Course.NameMaxLength)
                .IsRequired();

            builder.Property(c => c.Description)
                .HasMaxLength(Course.DescriptionMaxLength);

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
                .HasMaxLength(Semester.NameMaxLength)
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
                .HasMaxLength(Document.FileNameMaxLength)
                .IsRequired();

            builder.Property(d => d.ContentType)
                .HasMaxLength(Document.ContentTypeMaxLength)
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
    }
}
