# Getting Started

Install the package(s) you need and bootstrap the core services.

```bash
dotnet add package SquidStd.Services.Core
```

```csharp
using DryIoc;
using SquidStd.Services.Core.Extensions;

var container = new Container();

// config manager + event bus + jobs + timer wheel + dispatcher + metrics + storage + secrets
container.RegisterCoreServices("squidstd", Directory.GetCurrentDirectory());
```

From here, add focused packages as needed — see the per-package guides in the sidebar and the
[API reference](../api/index.md).
