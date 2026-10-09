using Microsoft.JSInterop;

namespace StudyHub.UI.Services;

public sealed class FileDownloadAccessor(IJSRuntime jsRuntime) : IFileDownloadAccessor
{
    public async Task DownloadAsync(string fileName, string contentType, byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var streamReference = new DotNetStreamReference(stream);
        await jsRuntime.InvokeVoidAsync(
            "studyHubDownload.save",
            fileName,
            contentType,
            streamReference
        );
    }
}
