<h1 align="center">SquidStd.Telemetry.Abstractions</h1>

Dependency-free telemetry configuration shared by SquidStd observability providers
(e.g. `SquidStd.Telemetry.OpenTelemetry`). Holds `TelemetryOptions`, the `OtlpProtocolType` enum, and
the `SquidStdActivity` helper / `SquidStd.*` ActivitySource naming convention. No OpenTelemetry
dependency: custom spans use the BCL `System.Diagnostics.ActivitySource`.

## Install

```bash
dotnet add package SquidStd.Telemetry.Abstractions
```

## Custom spans

```csharp
using SquidStd.Telemetry.Abstractions;

using var activity = SquidStdActivity.Source.StartActivity("checkout");
activity?.SetTag("order.id", orderId);
```

Subsystems create their own `new ActivitySource("SquidStd.Something")`; the OpenTelemetry provider
captures every `SquidStd.*` source automatically.
