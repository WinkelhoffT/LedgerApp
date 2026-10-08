namespace StudyHub.Logic.Domain;

/// <param name="LineNumber">1-based line the record starts on.</param>
internal sealed record AnkiCsvRecord(int LineNumber, IReadOnlyList<string> Fields);
