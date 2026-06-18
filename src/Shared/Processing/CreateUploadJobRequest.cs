namespace ApiPdfCsv.Shared.Processing;

public record CreateUploadJobRequest(
    string SessionId,
    string JobKind,
    string? FileType = null,
    string? InputFileName = null,
    string? MetadataJson = null);

public record UploadJobCompletion(
    string Message,
    string? OutputFile = null,
    string? ResultJson = null);
