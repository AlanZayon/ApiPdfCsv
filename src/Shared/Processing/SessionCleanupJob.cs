using ApiPdfCsv.Shared.Storage;
using Hangfire;

namespace ApiPdfCsv.Shared.Processing;

public class SessionCleanupJob
{
    private readonly IBlobStorageService _blobStorage;

    public SessionCleanupJob(IBlobStorageService blobStorage)
    {
        _blobStorage = blobStorage;
    }

    public async Task DeleteSessionAsync(string userId, string sessionId)
    {
        await _blobStorage.DeleteSessionAsync(userId, sessionId);
    }

    public static void Schedule(string userId, string sessionId, TimeSpan delay)
    {
        BackgroundJob.Schedule<SessionCleanupJob>(
            job => job.DeleteSessionAsync(userId, sessionId),
            delay);
    }
}
