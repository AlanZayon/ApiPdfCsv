using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace ApiPdfCsv.Shared.Storage;

public class S3BlobStorageService : IBlobStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly StorageOptions _options;

    public S3BlobStorageService(IAmazonS3 s3Client, IOptions<StorageOptions> options)
    {
        _s3Client = s3Client;
        _options = options.Value;
    }

    public async Task<string> SaveAsync(
        string userId,
        string sessionId,
        string fileName,
        Stream content,
        BlobScope scope = BlobScope.Output,
        CancellationToken cancellationToken = default)
    {
        var safeName = Path.GetFileName(fileName);
        var key = BuildKey(userId, sessionId, safeName, scope);

        var request = new PutObjectRequest
        {
            BucketName = _options.S3.BucketName,
            Key = key,
            InputStream = content,
            ContentType = "application/octet-stream"
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
        return safeName;
    }

    public async Task<Stream> OpenReadAsync(
        string userId,
        string sessionId,
        string fileName,
        BlobScope scope = BlobScope.Output,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(userId, sessionId, Path.GetFileName(fileName), scope);
        var response = await _s3Client.GetObjectAsync(_options.S3.BucketName, key, cancellationToken);
        var memory = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;
        return memory;
    }

    public async Task<bool> ExistsAsync(
        string userId,
        string sessionId,
        string fileName,
        BlobScope scope = BlobScope.Output,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var key = BuildKey(userId, sessionId, Path.GetFileName(fileName), scope);
            await _s3Client.GetObjectMetadataAsync(_options.S3.BucketName, key, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
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
            if (await ExistsAsync(userId, sessionId, safeName, BlobScope.Output, cancellationToken))
            {
                return safeName;
            }

            throw new FileNotFoundException($"File not found: {safeName}");
        }

        foreach (var preferred in new[] { "EXTRATO.csv", "PGTO.csv" })
        {
            if (await ExistsAsync(userId, sessionId, preferred, BlobScope.Output, cancellationToken))
            {
                return preferred;
            }
        }

        var prefix = BuildKey(userId, sessionId, string.Empty, BlobScope.Output);
        var listRequest = new ListObjectsV2Request
        {
            BucketName = _options.S3.BucketName,
            Prefix = prefix
        };

        var response = await _s3Client.ListObjectsV2Async(listRequest, cancellationToken);
        var latest = response.S3Objects
            .Where(o => !o.Key.EndsWith('/'))
            .OrderByDescending(o => o.LastModified)
            .FirstOrDefault();

        if (latest == null)
        {
            throw new FileNotFoundException("No output files found for session.");
        }

        return Path.GetFileName(latest.Key);
    }

    public async Task DeleteSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
    {
        foreach (var scope in new[] { BlobScope.Output, BlobScope.Upload })
        {
            var prefix = BuildKey(userId, sessionId, string.Empty, scope);
            var listRequest = new ListObjectsV2Request
            {
                BucketName = _options.S3.BucketName,
                Prefix = prefix
            };

            var response = await _s3Client.ListObjectsV2Async(listRequest, cancellationToken);
            foreach (var obj in response.S3Objects)
            {
                await _s3Client.DeleteObjectAsync(_options.S3.BucketName, obj.Key, cancellationToken);
            }
        }
    }

    private string BuildKey(string userId, string sessionId, string fileName, BlobScope scope)
    {
        var scopeFolder = scope == BlobScope.Output ? "outputs" : "uploads";
        var sanitizedUserId = SanitizeSegment(userId);
        var sanitizedSessionId = SanitizeSegment(sessionId);
        var prefix = _options.S3.Prefix?.Trim('/') ?? "outputs";

        if (string.IsNullOrEmpty(fileName))
        {
            return $"{prefix}/{scopeFolder}/{sanitizedUserId}/{sanitizedSessionId}/";
        }

        return $"{prefix}/{scopeFolder}/{sanitizedUserId}/{sanitizedSessionId}/{fileName}";
    }

    private static string SanitizeSegment(string segment)
    {
        return segment
            .Replace("..", string.Empty)
            .Replace("/", string.Empty)
            .Replace("\\", string.Empty);
    }
}
