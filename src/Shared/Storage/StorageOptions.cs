namespace ApiPdfCsv.Shared.Storage;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "Local";

    public int RetentionDays { get; set; } = 7;

    public S3StorageOptions S3 { get; set; } = new();
}

public class S3StorageOptions
{
    public string BucketName { get; set; } = string.Empty;

    public string Region { get; set; } = "sa-east-1";

    public string Prefix { get; set; } = "outputs";
}
