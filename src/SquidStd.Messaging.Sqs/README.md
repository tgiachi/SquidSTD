<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Messaging.Sqs</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Messaging.Sqs/"><img src="https://img.shields.io/nuget/v/SquidStd.Messaging.Sqs.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Messaging.Sqs.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/messaging-sqs.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

AWS SQS/SNS transport for SquidStd.Messaging. Implements `IQueueProvider` over SQS (with a redrive
policy to a dead-letter queue) and `ITopicProvider` via SNS+SQS fan-out, behind the same
`IMessageQueue` / `IMessageTopic` API as the other providers. Registered with a single
`AddSqsMessaging(...)` call. Connection details come from a shared `AwsConfigEntry`.

## Install

```bash
dotnet add package SquidStd.Messaging.Sqs
```

## Usage

```csharp
using DryIoc;
using SquidStd.Aws.Abstractions.Data.Config;
using SquidStd.Messaging.Sqs.Data.Config;
using SquidStd.Messaging.Sqs.Extensions;

var container = new Container();
container.AddSqsMessaging(new SqsOptions { Aws = new AwsConfigEntry { Region = "eu-west-1" } });
// or: container.AddSqsMessaging("sqs://accessKey:secretKey@eu-west-1?endpoint=http://localhost:4566");
```

- Queues are created on first use with a redrive policy to `<queue><deadLetterSuffix>` (max receive
  count = `MessagingOptions.MaxDeliveryAttempts`).
- Topics map to SNS; each subscriber gets a dedicated SQS queue subscribed with raw message delivery.
- Names are sanitized to the SQS/SNS alphabet (the default `.dlq` suffix becomes `-dlq`).
- Payloads travel base64-encoded in the message body.

## Key types

| Type                                | Purpose                                  |
|-------------------------------------|------------------------------------------|
| `SqsMessagingRegistrationExtensions` | `AddSqsMessaging(...)` registration.    |
| `SqsQueueProvider`                  | SQS-backed `IQueueProvider`.             |
| `SqsTopicProvider`                  | SNS+SQS-backed `ITopicProvider`.         |
| `SqsOptions`                        | AWS connection + queue/topic configuration. |

## Related

- Tutorial: [Messaging](https://tgiachi.github.io/squid-std/tutorials/messaging.html)

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
</content>
