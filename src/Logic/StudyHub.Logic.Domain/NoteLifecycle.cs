using StudyHub.Logic.Domain.Contract;
using StudyHub.Shared.Notes;

namespace StudyHub.Logic.Domain;

public sealed class NoteLifecycle : INoteLifecycle
{
    public Note Create(string title, string content, string? tags, Guid? courseId, Guid? semesterId)
    {
        var validatedTitle = ValidateTitle(title);
        var validatedContent = ValidateContent(content);
        var validatedTags = ValidateTags(tags);
        ValidateParent(courseId, semesterId);
        var now = DateTime.UtcNow;

        return new Note(
            Id: Guid.NewGuid(),
            Title: validatedTitle,
            Content: validatedContent,
            Tags: validatedTags,
            CourseId: courseId,
            SemesterId: semesterId,
            IsArchived: false,
            CreatedAt: now,
            UpdatedAt: now);
    }

    public Note Update(Note note, string title, string content, string? tags, Guid? courseId, Guid? semesterId)
    {
        if (note.IsArchived)
        {
            throw new NoteArchivedException(note.Id);
        }

        var validatedTitle = ValidateTitle(title);
        var validatedContent = ValidateContent(content);
        var validatedTags = ValidateTags(tags);
        ValidateParent(courseId, semesterId);

        return note with
        {
            Title = validatedTitle,
            Content = validatedContent,
            Tags = validatedTags,
            CourseId = courseId,
            SemesterId = semesterId,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public Note Archive(Note note) =>
        note.IsArchived
            ? note
            : note with { IsArchived = true, UpdatedAt = DateTime.UtcNow };

    public Note Restore(Note note) =>
        !note.IsArchived
            ? note
            : note with { IsArchived = false, UpdatedAt = DateTime.UtcNow };

    private static string ValidateTitle(string title)
    {
        var trimmedTitle = title?.Trim() ?? string.Empty;
        if (trimmedTitle.Length == 0)
        {
            throw new NoteValidationException("Note title is required.");
        }

        if (trimmedTitle.Length > Note.TitleMaxLength)
        {
            throw new NoteValidationException($"Note title must not exceed {Note.TitleMaxLength} characters.");
        }

        return trimmedTitle;
    }

    private static string ValidateContent(string content)
    {
        if (content is null || content.Length > Note.ContentMaxLength)
        {
            throw new NoteValidationException($"Note content must not exceed {Note.ContentMaxLength} characters.");
        }

        return content;
    }

    private static string? ValidateTags(string? tags)
    {
        var trimmedTags = tags?.Trim();
        if (trimmedTags is { Length: > Note.TagsMaxLength })
        {
            throw new NoteValidationException($"Note tags must not exceed {Note.TagsMaxLength} characters.");
        }

        return string.IsNullOrEmpty(trimmedTags) ? null : trimmedTags;
    }

    private static void ValidateParent(Guid? courseId, Guid? semesterId)
    {
        if (courseId is null == semesterId is null)
        {
            throw new NoteValidationException("A note must be assigned to exactly one of a course or a semester.");
        }
    }
}
