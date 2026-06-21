using System.Text.Json;
using ApiPdfCsv.CrossCutting.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiPdfCsv.Shared.Processing;

public enum UploadJobState
{
    Pending,
    Processing,
    Completed,
    Failed
}

public record UploadJobStatus(
    string JobId,
    UploadJobState State,
    string? Message = null,
    DateTime CreatedAtUtc = default,
    DateTime? CompletedAtUtc = null,
    string? Type = null,
    string? OutputFile = null,
    JsonElement? Result = null,
    string? ProgressHint = null,
    string? SessionId = null,
    string? InputFileName = null,
    string? MetadataJson = null);

public record UploadJobHistoryItem(
    string JobId,
    UploadJobState State,
    string? FileType,
    string? InputFileName,
    string? OutputFile,
    string? Message,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    int? ClienteId,
    string? ClienteNome,
    string? Cnpj,
    string SessionId);

public interface IUploadJobService
{
    Task<string> CreateJobAsync(string userId, CreateUploadJobRequest request, CancellationToken cancellationToken = default);
    Task MarkProcessingAsync(string jobId, CancellationToken cancellationToken = default);
    Task MarkCompletedAsync(string jobId, UploadJobCompletion completion, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(string jobId, string message, CancellationToken cancellationToken = default);
    Task<UploadJobStatus?> GetStatusAsync(string jobId, string userId, CancellationToken cancellationToken = default);
    Task<UploadJob?> GetJobEntityAsync(string jobId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<UploadJobHistoryItem> Items, int Total)> ListHistoryAsync(
        string userId,
        int? clienteId,
        string? tipo,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<UploadJob?> GetJobForDownloadAsync(string jobId, string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UploadJob>> ListExpiredJobsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
    Task PurgeExpiredJobsAsync(int retentionDays, CancellationToken cancellationToken = default);
}
