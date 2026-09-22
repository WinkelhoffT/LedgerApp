using StudyHub.Shared.Notes;

namespace StudyHub.Shared.Domain.Notes;

public sealed record Note
{
    // Defines what "the same title" means for uniqueness comparisons (trim + case-fold). Exposed
    // as a delegate so StudyHub.Data's NoteRepository can reuse this domain rule instead of
    // re-implementing title normalization itself.
    public static readonly Func<string, string> NormalizeTitleForComparison = title => title.Trim().ToLower();

    private Note()
    {
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public string? Tags { get; private set; }

    public Guid? CourseId { get; private set; }

    public Guid? SemesterId { get; private set; }

    public bool IsArchived { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public static Note Create(string title, string content, string? tags, Guid? courseId, Guid? semesterId)
    {
        var note = new Note
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
        };

        note.SetDetails(title, content, tags, courseId, semesterId);

        return note;
    }

    public void Update(string title, string content, string? tags, Guid? courseId, Guid? semesterId)
    {
        if (IsArchived)
        {
            throw new NoteArchivedException(Id);
        }

        SetDetails(title, content, tags, courseId, semesterId);
    }

    public void Archive()
    {
        if (IsArchived)
        {
            return;
        }

        IsArchived = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        if (!IsArchived)
        {
            return;
        }

        IsArchived = false;
        UpdatedAt = DateTime.UtcNow;
    }

    private void SetDetails(string title, string content, string? tags, Guid? courseId, Guid? semesterId)
    {
        var trimmedTitle = title?.Trim() ?? string.Empty;
        if (trimmedTitle.Length == 0)
        {
            throw new NoteValidationException("Note title is required.");
        }

        if (trimmedTitle.Length > CreateNoteRequest.TitleMaxLength)
        {
            throw new NoteValidationException($"Note title must not exceed {CreateNoteRequest.TitleMaxLength} characters.");
        }

        if (content is null || content.Length > CreateNoteRequest.ContentMaxLength)
        {
            throw new NoteValidationException($"Note content must not exceed {CreateNoteRequest.ContentMaxLength} characters.");
        }

        var trimmedTags = tags?.Trim();
        if (trimmedTags is { Length: > CreateNoteRequest.TagsMaxLength })
        {
            throw new NoteValidationException($"Note tags must not exceed {CreateNoteRequest.TagsMaxLength} characters.");
        }

        if (courseId is null == semesterId is null)
        {
            throw new NoteValidationException("A note must be assigned to exactly one of a course or a semester.");
        }

        Title = trimmedTitle;
        Content = content;
        Tags = string.IsNullOrEmpty(trimmedTags) ? null : trimmedTags;
        CourseId = courseId;
        SemesterId = semesterId;
        UpdatedAt = DateTime.UtcNow;
    }
}
