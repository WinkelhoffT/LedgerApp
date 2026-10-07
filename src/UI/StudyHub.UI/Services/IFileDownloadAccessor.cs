namespace StudyHub.UI.Services;

/// <summary>
/// JS-interop access for handing in-memory file content to the browser as a download. Used where
/// the content only exists in the circuit (e.g. exported flashcards), so a GET download endpoint
/// like the document download proxy doesn't fit.
/// </summary>
public interface IFileDownloadAccessor
{
    Task DownloadAsync(string fileName, string contentType, byte[] content);
}
