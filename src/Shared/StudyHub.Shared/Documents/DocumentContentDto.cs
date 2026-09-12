namespace StudyHub.Shared.Documents;

public sealed record DocumentContentDto(string FileName, string ContentType, byte[] Content);
