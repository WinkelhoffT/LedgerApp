namespace StudyHub.Logic.Domain.Notes;

public sealed class Note
{
    public const int TitleMaxLength = 200;

    private Note()
    {
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public Guid CourseId { get; private set; }

    public bool IsArchived { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public static Note Create(string title, string? content, Guid courseId)
    {
        var note = new Note
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
        };

        note.SetDetails(title, content, courseId);

        return note;
    }

    public void Update(string title, string? content, Guid courseId)
    {
        if (IsArchived)
        {
            throw new NoteArchivedException(Id);
        }

        SetDetails(title, content, courseId);
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

    private void SetDetails(string title, string? content, Guid courseId)
    {
        var trimmedTitle = title?.Trim() ?? string.Empty;
        if (trimmedTitle.Length == 0)
        {
            throw new NoteValidationException("Note title is required.");
        }

        if (trimmedTitle.Length > TitleMaxLength)
        {
            throw new NoteValidationException($"Note title must not exceed {TitleMaxLength} characters.");
        }

        if (courseId == Guid.Empty)
        {
            throw new NoteValidationException("Note must be assigned to a course.");
        }

        Title = trimmedTitle;
        Content = content ?? string.Empty;
        CourseId = courseId;
        UpdatedAt = DateTime.UtcNow;
    }
}
