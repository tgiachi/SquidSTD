# Email: receive, send, and queue

Poll a mailbox for inbound mail, send outbound mail over SMTP, and queue mail for background delivery.

## What you'll build

A host using `SquidStd.Mail.MailKit` and `SquidStd.Mail.Queue`: an IMAP poller that raises a `MailReceivedEvent`
for each new email, an SMTP sender (`IMailSender`), and a fire-and-forget send queue (`IMailQueue`) backed by
messaging. The contracts live in `SquidStd.Mail.Abstractions`.

## Prerequisites

- .NET 10 SDK
- `dotnet add package SquidStd.Mail.MailKit`
- `dotnet add package SquidStd.Mail.Queue`
- Real IMAP/SMTP credentials are only needed to actually connect; the sample compiles and runs without them.

## Steps

### 1. Receive: poll a mailbox

`AddMail` registers an IMAP/POP3 poller; each received message is published as a `MailReceivedEvent`, which an
`IAsyncEventListener<MailReceivedEvent>` handles.

[!code-csharp[](../../samples/SquidStd.Samples.Email/Program.cs#step-1)]

### 2. Send: outbound SMTP

`AddMailSender` registers `IMailSender`; build an `OutgoingMailMessage` and call `SendAsync`.

[!code-csharp[](../../samples/SquidStd.Samples.Email/Program.cs#step-2)]

### 3. Queue: background delivery

`AddInMemoryMessaging` plus `AddMailQueue` register `IMailQueue`; `EnqueueAsync` hands the message to a
background consumer that sends it via the SMTP sender.

[!code-csharp[](../../samples/SquidStd.Samples.Email/Program.cs#step-3)]

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.Email
```

It registers the poller, sender, and queue, registers the received-mail listener, and enqueues a message.
Pass `--send` to also attempt a synchronous SMTP send (requires a reachable server).

## How it works

The poller runs on the timer wheel, fetches new messages over IMAP/POP3 with MailKit, and publishes a
`MailReceivedEvent` on the event bus. `IMailSender` maps `OutgoingMailMessage` to a MIME message and sends it.
`AddMailQueue` layers a queue over `SquidStd.Messaging`: `EnqueueAsync` publishes to the queue and a background
consumer (`MailSendConsumerService`) drains it through `IMailSender`, decoupling request latency from delivery.

## See also

- [SquidStd.Mail.MailKit reference](../articles/mail-mailkit.md)
- [SquidStd.Mail.Queue reference](../articles/mail-queue.md)
- [SquidStd.Mail.Abstractions reference](../articles/mail-abstractions.md)
