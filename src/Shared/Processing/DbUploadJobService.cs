using System.Text.Json;
using ApiPdfCsv.CrossCutting.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiPdfCsv.Shared.Processing;

public class DbUploadJobService : IUploadJobService
{
    private readonly AppDbContext _dbContext;

    public DbUploadJobService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> CreateJobAsync(string userId, CreateUploadJobRequest request, CancellationToken cancellationToken = default)
    {
        var jobId = Guid.NewGuid().ToString("N");
        var job = new UploadJob
        {
            Id = jobId,
            UserId = userId,
            SessionId = request.SessionId,
            State = UploadJobState.Pending,
            JobKind = request.JobKind,
            FileType = request.FileType,
            InputFileName = request.InputFileName,
            MetadataJson = request.MetadataJson,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.UploadJobs.Add(job);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return jobId;
    }

    public async Task MarkProcessingAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.UploadJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        if (job == null) return;

        job.State = UploadJobState.Processing;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkCompletedAsync(string jobId, UploadJobCompletion completion, CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.UploadJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        if (job == null) return;

        job.State = UploadJobState.Completed;
        job.Message = completion.Message;
        job.OutputFile = completion.OutputFile;
        job.ResultJson = completion.ResultJson;
        job.CompletedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(string jobId, string message, CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.UploadJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        if (job == null) return;

        job.State = UploadJobState.Failed;
        job.Message = message;
        job.CompletedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<UploadJobStatus?> GetStatusAsync(string jobId, string userId, CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.UploadJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId && j.UserId == userId, cancellationToken);

        if (job == null) return null;

        JsonElement? result = null;
        if (!string.IsNullOrWhiteSpace(job.ResultJson))
        {
            using var doc = JsonDocument.Parse(job.ResultJson);
            result = doc.RootElement.Clone();
        }

        return new UploadJobStatus(
            job.Id,
            job.State,
            job.Message,
            job.CreatedAtUtc,
            job.CompletedAtUtc,
            job.FileType,
            job.OutputFile,
            result);
    }

    public Task<UploadJob?> GetJobEntityAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return _dbContext.UploadJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
    }
}
