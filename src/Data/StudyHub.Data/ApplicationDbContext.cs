using Microsoft.EntityFrameworkCore;
using StudyHub.Logic.Domain.Courses;

namespace StudyHub.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();

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
    }
}
