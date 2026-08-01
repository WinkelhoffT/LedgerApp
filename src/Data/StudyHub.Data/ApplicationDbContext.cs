using Microsoft.EntityFrameworkCore;
using StudyHub.Logic.Domain.Courses;
using StudyHub.Logic.Domain.Semesters;

namespace StudyHub.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();

    public DbSet<Semester> Semesters => Set<Semester>();

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

            builder.Property(c => c.IsArchived)
                .IsRequired();

            builder.Property(c => c.CreatedAt)
                .IsRequired();

            builder.Property(c => c.UpdatedAt)
                .IsRequired();

            builder.HasIndex(c => c.Name)
                .IsUnique();
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
    }
}
