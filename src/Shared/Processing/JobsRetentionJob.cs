using ApiPdfCsv.Shared.Storage;
using Hangfire;

namespace ApiPdfCsv.Shared.Processing;

public class JobsRetentionJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBlobStorageService _blobStorage;

    public JobsRetentionJob(IServiceScopeFactory scopeFactory, IBlobStorageService blobStorage)
    {
        _scopeFactory = scopeFactory;
        _blobStorage = blobStorage;
    }

    public async Task PurgeExpiredAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<IUploadJobService>();
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

        var jobs = await jobService.ListExpiredJobsAsync(cutoff, cancellationToken);
        foreach (var job in jobs)
        {
            await _blobStorage.DeleteSessionAsync(job.UserId, job.SessionId);
        }

        await jobService.PurgeExpiredJobsAsync(retentionDays, cancellationToken);
    }

    public static void RegisterRecurring(int retentionDays)
    {
        RecurringJob.AddOrUpdate<JobsRetentionJob>(
            "purge-expired-jobs",
            job => job.PurgeExpiredAsync(retentionDays, CancellationToken.None),
            Cron.Weekly);
    }
}
