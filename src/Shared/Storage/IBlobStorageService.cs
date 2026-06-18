namespace ApiPdfCsv.Shared.Storage;

public interface IBlobStorageService
{
    Task<string> SaveAsync(
        string userId,
        string sessionId,
        string fileName,
        Stream content,
        BlobScope scope = BlobScope.Output,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(
        string userId,
        string sessionId,
        string fileName,
        BlobScope scope = BlobScope.Output,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string userId,
        string sessionId,
        string fileName,
        BlobScope scope = BlobScope.Output,
        CancellationToken cancellationToken = default);

    Task<string> ResolveOutputFileNameAsync(
        string userId,
        string sessionId,
        string? fileName = null,
        CancellationToken cancellationToken = default);

    Task DeleteSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default);
}
