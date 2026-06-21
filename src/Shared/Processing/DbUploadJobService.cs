using System.Text.Json;
using ApiPdfCsv.CrossCutting.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiPdfCsv.Shared.Processing;

public class DbUploadJobService : IUploadJobService
{
    private readonly AppDbContext _dbContext;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

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
            result,
            ResolveProgressHint(job),
            job.SessionId,
            job.InputFileName,
            job.MetadataJson);
    }

    public Task<UploadJob?> GetJobEntityAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return _dbContext.UploadJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
    }

    public async Task<(IEnumerable<UploadJobHistoryItem> Items, int Total)> ListHistoryAsync(
        string userId,
        int? clienteId,
        string? tipo,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.UploadJobs
            .AsNoTracking()
            .Where(j => j.UserId == userId && j.JobKind == "upload");

        if (!string.IsNullOrWhiteSpace(tipo))
            query = query.Where(j => j.FileType == tipo.Trim().TrimStart('.'));

        if (from.HasValue)
            query = query.Where(j => j.CreatedAtUtc >= from.Value.ToUniversalTime());

        if (to.HasValue)
            query = query.Where(j => j.CreatedAtUtc <= to.Value.ToUniversalTime());

        var jobs = await query
            .OrderByDescending(j => j.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var mapped = jobs
            .Select(j =>
            {
                var meta = DeserializeMetadata(j.MetadataJson);
                return new UploadJobHistoryItem(
                    j.Id,
                    j.State,
                    j.FileType,
                    meta?.InputOriginalFileName ?? j.InputFileName,
                    j.OutputFile,
                    j.Message,
                    j.CreatedAtUtc,
                    j.CompletedAtUtc,
                    meta?.ClienteId,
                    meta?.ClienteNome,
                    meta?.Cnpj,
                    j.SessionId);
            })
            .Where(item => !clienteId.HasValue || item.ClienteId == clienteId)
            .ToList();

        var total = mapped.Count;
        var items = mapped
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (items, total);
    }

    public Task<UploadJob?> GetJobForDownloadAsync(string jobId, string userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.UploadJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(
                j => j.Id == jobId && j.UserId == userId && j.State == UploadJobState.Completed,
                cancellationToken);
    }

    public async Task<IReadOnlyList<UploadJob>> ListExpiredJobsAsync(
        DateTime cutoffUtc,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UploadJobs
            .AsNoTracking()
            .Where(j => j.CreatedAtUtc < cutoffUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task PurgeExpiredJobsAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var expired = await _dbContext.UploadJobs
            .Where(j => j.CreatedAtUtc < cutoff)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0) return;

        _dbContext.UploadJobs.RemoveRange(expired);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? ResolveProgressHint(UploadJob job)
    {
        return job.State switch
        {
            UploadJobState.Pending => "queued",
            UploadJobState.Processing when job.FileType == "pdf" => "parsing_pdf",
            UploadJobState.Processing when job.FileType == "ofx" => "parsing_ofx",
            UploadJobState.Completed => "completed",
            UploadJobState.Failed => "failed",
            _ => null
        };
    }

    private static UploadJobMetadata? DeserializeMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson)) return null;
        return JsonSerializer.Deserialize<UploadJobMetadata>(metadataJson, JsonOptions);
    }
}
