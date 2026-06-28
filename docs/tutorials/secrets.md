# Secrets (KMS / Secrets Manager)

Wire an AWS KMS-backed secret protector and a Secrets Manager store, then envelope-encrypt and
store values.

## What you'll build

A host that registers `ISecretProtector` (KMS envelope encryption) and `ISecretStore`
(AWS Secrets Manager) from `SquidStd.Secrets.Aws`, then exercises protect/unprotect and
set/get/list.

## Prerequisites

- .NET 10 SDK
- `dotnet add package SquidStd.Secrets.Aws`
- The live calls (steps 2–3) need a KMS + Secrets Manager endpoint. The sample is
  compile-and-wire focused: it resolves the services without AWS and only runs the live calls
  when `SQUIDSTD_RUN_AWS=1` is set with [LocalStack](https://localstack.cloud) (or real AWS)
  reachable at the configured endpoint.

## Steps

### 1. Register the KMS protector and Secrets Manager store

`RegisterKmsSecretProtector` and `RegisterAwsSecretsManagerStore` take the KMS key alias, the
secret name prefix, and the AWS endpoint — pointed here at a LocalStack `ServiceUrl`.

[!code-csharp[](../../samples/SquidStd.Samples.Secrets/Program.cs#step-1)]

### 2. Envelope-encrypt a value

`Protect` requests a KMS data key, encrypts the payload with it, and wraps the encrypted data
key alongside the ciphertext; `Unprotect` reverses it.

[!code-csharp[](../../samples/SquidStd.Samples.Secrets/Program.cs#step-2)]

### 3. Store, fetch and list secrets

`ISecretStore` reads and writes named secrets through Secrets Manager; `ListNamesAsync` streams
the names under the configured prefix.

[!code-csharp[](../../samples/SquidStd.Samples.Secrets/Program.cs#step-3)]

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.Secrets
# with the live calls:
SQUIDSTD_RUN_AWS=1 dotnet run --project samples/SquidStd.Samples.Secrets
```

## Next steps

- [Security guide](../articles/guides/security.md)
- [SquidStd.Secrets.Aws reference](../articles/secrets-aws.md)
