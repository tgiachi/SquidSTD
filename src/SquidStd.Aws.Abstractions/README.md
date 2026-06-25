<h1 align="center">SquidStd.Aws.Abstractions</h1>

Shared, dependency-free AWS connection config for SquidStd providers built on the AWS SDK
(e.g. `SquidStd.Messaging.Sqs`). A single `AwsConfigEntry` carries region, credentials and an
optional endpoint override (for LocalStack or other S3/SQS-compatible endpoints).

## Install

```bash
dotnet add package SquidStd.Aws.Abstractions
```

## Usage

```csharp
using SquidStd.Aws.Abstractions.Data.Config;

var aws = new AwsConfigEntry
{
    Region = "eu-west-1",
    // AccessKey/SecretKey omitted -> the AWS default credential chain is used
    ServiceUrl = null,        // set to http://localhost:4566 for LocalStack
};
```

When `AccessKey`/`SecretKey` are null, consumers fall back to the AWS default credential chain
(environment variables, shared profile, IAM role). When `ServiceUrl` is set, consumers point their
client at that endpoint instead of the regional AWS endpoint.
