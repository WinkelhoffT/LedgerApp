namespace StudyHub.Logic.Business.Documents;

public sealed class DocumentTooLargeException(long sizeBytes, long maxSizeBytes)
    : Exception($"Document size of {sizeBytes} bytes exceeds the maximum allowed size of {maxSizeBytes} bytes.")
{
    public long SizeBytes { get; } = sizeBytes;

    public long MaxSizeBytes { get; } = maxSizeBytes;
}
