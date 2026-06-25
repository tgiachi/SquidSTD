namespace SquidStd.Aws.Abstractions.Data.Config;

/// <summary>
/// Shared connection configuration for AWS-style services (region, credentials, endpoint override).
/// A dependency-free POCO: each provider maps it to its own SDK (AWS SDK, MinIO SDK, ...).
/// </summary>
public sealed class AwsConfigEntry
{
    /// <summary>AWS region, e.g. "eu-west-1". Default "us-east-1".</summary>
    public string Region { get; init; } = "us-east-1";

    /// <summary>Access key. When null, the AWS default credential chain is used.</summary>
    public string? AccessKey { get; init; }

    /// <summary>Secret key. When null, the AWS default credential chain is used.</summary>
    public string? SecretKey { get; init; }

    /// <summary>Optional STS session token.</summary>
    public string? SessionToken { get; init; }

    /// <summary>Optional endpoint override (full URL): LocalStack http://localhost:4566, MinIO http://localhost:9000.</summary>
    public string? ServiceUrl { get; init; }
}
