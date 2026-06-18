using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;

namespace ApiPdfCsv.Shared.Storage;

public class LocalBlobStorageService : IBlobStorageService
{
    private readonly IFileService _fileService;

    public LocalBlobStorageService(IFileService fileService)
    {
        _fileService = fileService;
    }

    public async Task<string> SaveAsync(
        string userId,
        string sessionId,
        string fileName,
        Stream content,
        BlobScope scope = BlobScope.Output,
        CancellationToken cancellationToken = default)
    {
        var dir = GetScopeDir(userId, sessionId, scope);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, Path.GetFileName(fileName));
        await using var fileStream = File.Create(path);
        await content.CopyToAsync(fileStream, cancellationToken);
        return Path.GetFileName(fileName);
    }

    public Task<Stream> OpenReadAsync(
        string userId,
        string sessionId,
        string fileName,
        BlobScope scope = BlobScope.Output,
        CancellationToken cancellationToken = default)
    {
        if (scope == BlobScope.Output)
        {
            var path = _fileService.GetUserFile(userId, sessionId, fileName);
            return Task.FromResult<Stream>(File.OpenRead(path));
        }

        var uploadPath = Path.Combine(GetScopeDir(userId, sessionId, scope), Path.GetFileName(fileName));
        if (!File.Exists(uploadPath))
        {
            throw new FileNotFoundException($"File not found: {fileName}");
        }

        return Task.FromResult<Stream>(File.OpenRead(uploadPath));
    }

    public Task<bool> ExistsAsync(
        string userId,
        string sessionId,
        string fileName,
        BlobScope scope = BlobScope.Output,
        CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(GetScopeDir(userId, sessionId, scope), Path.GetFileName(fileName));
        return Task.FromResult(File.Exists(path));
    }

    public async Task<string> ResolveOutputFileNameAsync(
        string userId,
        string sessionId,
        string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var safeName = Path.GetFileName(fileName);
            if (await ExistsAsync(userId, sessionId, safeName))
            {
                return safeName;
            }

            throw new FileNotFoundException($"File not found: {safeName}");
        }

        foreach (var preferred in new[] { "EXTRATO.csv", "PGTO.csv" })
        {
            if (await ExistsAsync(userId, sessionId, preferred))
            {
                return preferred;
            }
        }

        var dir = GetScopeDir(userId, sessionId, BlobScope.Output);
        if (!Directory.Exists(dir))
        {
            throw new FileNotFoundException("No output files found for session.");
        }

        var latest = Directory.GetFiles(dir)
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .FirstOrDefault();

        if (latest == null)
        {
            throw new FileNotFoundException("No output files found for session.");
        }

        return latest.Name;
    }

    public Task DeleteSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
    {
        _fileService.ClearUserFiles(userId, sessionId);
        return Task.CompletedTask;
    }

    private string GetScopeDir(string userId, string sessionId, BlobScope scope)
    {
        return scope == BlobScope.Output
            ? _fileService.GetUserOutputDir(userId, sessionId)
            : _fileService.GetUserUploadDir(userId, sessionId);
    }
}
