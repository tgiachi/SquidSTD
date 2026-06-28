<h1 align="center">SquidStd.Secrets.Aws</h1>

AWS adapters for the two SquidStd secret seams: an `ISecretProtector` that envelope-encrypts payloads
with **AWS KMS** data keys, and an `ISecretStore` backed by **AWS Secrets Manager**. Drop either into the
container in place of the file-backed defaults to push key custody and secret storage into AWS-managed
services, keeping the rest of the application unchanged.

## Install

```bash
dotnet add package SquidStd.Secrets.Aws
```

## Usage

```csharp
using SquidStd.Secrets.Aws.Extensions;

// KMS-backed ISecretProtector (envelope encryption)
container.RegisterKmsSecretProtector(o =>
{
    o.KeyId    = "alias/my-app";   // KMS key id, ARN, or alias
    o.Aws.Region = "eu-west-1";
});

// Secrets Manager-backed ISecretStore
container.RegisterAwsSecretsManagerStore(o =>
{
    o.NamePrefix = "my-app/";       // optional logical namespace
    o.Aws.Region = "eu-west-1";
});
```

Both seams are then resolved like any other SquidStd secret service:

```csharp
var protector = container.Resolve<ISecretProtector>();
byte[] sealed   = protector.Protect(payload);
byte[] restored = protector.Unprotect(sealed);

var store = container.Resolve<ISecretStore>();
await store.SetAsync("db/main", connectionString);
string? value = await store.GetAsync("db/main");
await foreach (var name in store.ListNamesAsync("db/")) { /* ... */ }
```

## Key types

| Type | Purpose |
|------|---------|
| `KmsSecretProtector` | `ISecretProtector` using AWS KMS data keys (envelope encryption). |
| `AwsSecretsManagerStore` | `ISecretStore` backed by AWS Secrets Manager. |
| `KmsSecretProtectorOptions` | KMS key id/ARN/alias, region and credentials. |
| `AwsSecretsManagerOptions` | Optional name prefix, region and credentials. |

## Notes

- **Envelope encryption** — `KmsSecretProtector` calls `GenerateDataKey` per `Protect`, encrypts the payload
  locally with AES-256-GCM, and frames the KMS-wrapped data key alongside the ciphertext. The plaintext
  data key is zeroed immediately after use; `Unprotect` calls `Decrypt` to unwrap it. KMS never sees the
  payload, and large payloads are not bound by the 4 KB KMS direct-encrypt limit.
- **NamePrefix** — `AwsSecretsManagerStore` prepends `NamePrefix` to every secret id, giving each
  application its own namespace inside a shared account. `ListNamesAsync` strips the prefix so callers
  always see logical names. `Delete` returns `false` for a missing secret.
- **Credentials** — when `Aws.AccessKey` / `Aws.SecretKey` are omitted, the AWS SDK default credential
  chain is used (environment, shared profile, EC2/ECS role). Set `Aws.ServiceUrl` to target LocalStack
  or another compatible endpoint.
- **Tested against LocalStack** — the KMS and Secrets Manager adapters are covered by integration tests
  running on a `localstack/localstack` container.

## Related

- Tutorial: [Secrets (KMS / Secrets Manager)](https://tgiachi.github.io/squid-std/tutorials/secrets.html)

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
