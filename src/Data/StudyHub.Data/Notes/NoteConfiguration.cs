using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyHub.Logic.Domain.Courses;
using StudyHub.Logic.Domain.Notes;

namespace StudyHub.Data.Notes;

public sealed class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("Notes");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
            .HasMaxLength(Note.TitleMaxLength)
            .IsRequired();

        builder.Property(n => n.Content)
            .IsRequired();

        builder.Property(n => n.CourseId)
            .IsRequired();

        builder.Property(n => n.IsArchived)
            .IsRequired();

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        builder.Property(n => n.UpdatedAt)
            .IsRequired();

        builder.HasIndex(n => n.CourseId);

        // Restrict, not Cascade: notes are never hard-deleted (only archived), so a physical
        // delete of a Course should never silently take its Notes with it.
        builder.HasOne<Course>()
            .WithMany()
            .HasForeignKey(n => n.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
