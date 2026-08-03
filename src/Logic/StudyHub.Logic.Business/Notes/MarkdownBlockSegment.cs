namespace StudyHub.Logic.Business.Notes;

public sealed record MarkdownBlockSegment(bool IsCode, string Language, string Text);
