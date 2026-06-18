namespace ApiPdfCsv.Shared.Processing;

public class UploadJob
{
    public string Id { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string SessionId { get; set; } = string.Empty;

    public UploadJobState State { get; set; } = UploadJobState.Pending;

    public string JobKind { get; set; } = "upload";

    public string? FileType { get; set; }

    public string? InputFileName { get; set; }

    public string? OutputFile { get; set; }

    public string? Message { get; set; }

    public string? ResultJson { get; set; }

    public string? MetadataJson { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; set; }
}
