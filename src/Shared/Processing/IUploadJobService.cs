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
    JsonElement? Result = null);

public interface IUploadJobService
{
    Task<string> CreateJobAsync(string userId, CreateUploadJobRequest request, CancellationToken cancellationToken = default);
    Task MarkProcessingAsync(string jobId, CancellationToken cancellationToken = default);
    Task MarkCompletedAsync(string jobId, UploadJobCompletion completion, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(string jobId, string message, CancellationToken cancellationToken = default);
    Task<UploadJobStatus?> GetStatusAsync(string jobId, string userId, CancellationToken cancellationToken = default);
    Task<UploadJob?> GetJobEntityAsync(string jobId, CancellationToken cancellationToken = default);
}
