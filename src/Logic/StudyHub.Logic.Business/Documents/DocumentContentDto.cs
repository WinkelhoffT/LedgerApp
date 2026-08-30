namespace StudyHub.Logic.Business.Documents;

public sealed record DocumentContentDto(string FileName, string ContentType, byte[] Content);
