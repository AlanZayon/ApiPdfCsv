using Amazon.S3;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ApiPdfCsv.Shared.Storage;

public class S3StorageHealthCheck : IHealthCheck
{
    private readonly IAmazonS3 _s3Client;
    private readonly StorageOptions _options;

    public S3StorageHealthCheck(IAmazonS3 s3Client, IOptions<StorageOptions> options)
    {
        _s3Client = s3Client;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.S3.BucketName))
        {
            return HealthCheckResult.Unhealthy("S3 bucket not configured");
        }

        try
        {
            await _s3Client.GetBucketLocationAsync(_options.S3.BucketName, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message, ex);
        }
    }
}
