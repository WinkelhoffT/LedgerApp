namespace StudyHub.UI.Documents;

public static class FileSizeFormatter
{
    public static string Format(long bytes) => bytes switch
    {
        >= 1024 * 1024 => $"{bytes / (1024.0 * 1024):0.#} MB",
        >= 1024 => $"{bytes / 1024.0:0.#} KB",
        _ => $"{bytes} B",
    };
}
