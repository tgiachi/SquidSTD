<p align="center">
  <img src="https://raw.githubusercontent.com/tgiachi/squid-std/main/assets/icon.png" alt="SquidStd" width="120" height="120" />
</p>

<h1 align="center">SquidStd.Templating</h1>

<p align="center">
  <a href="https://www.nuget.org/packages/SquidStd.Templating/"><img src="https://img.shields.io/nuget/v/SquidStd.Templating.svg" alt="NuGet" /></a>
  <img src="https://img.shields.io/nuget/dt/SquidStd.Templating.svg" alt="Downloads" />
  <a href="https://tgiachi.github.io/squid-std/articles/templating.html"><img src="https://img.shields.io/badge/docs-DocFX-1390A3.svg" alt="docs" /></a>
  <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="license" />
</p>

Templating for SquidStd, backed by [Scriban](https://github.com/scriban/scriban). `ITemplateRenderer`
renders ad-hoc template strings and named templates (compiled and cached), and auto-loads
`*.tmpl` files from the `templates/` directory at startup.

## Install

```bash
dotnet add package SquidStd.Templating
```

## Features

- `ITemplateRenderer.RenderAsync(template, model)` — render an ad-hoc template string.
- `Register(name, template)` + `RenderByNameAsync(name, model)` — compiled, cached named templates.
- Startup auto-load: every `templates/**/*.tmpl` is registered by relative path without extension (`emails/welcome.tmpl` → `emails/welcome`).
- Scriban default member naming (`snake_case`): a `UserName` property is `{{ user.user_name }}`.
- Parse/render failures surface as `TemplateException`; unknown names as `InvalidOperationException`.

## Usage

```csharp
using DryIoc;
using SquidStd.Templating.Extensions;
using SquidStd.Templating.Interfaces;

container.AddTemplating(); // after RegisterCoreServices (a shared DirectoriesConfig is registered there)

var renderer = container.Resolve<ITemplateRenderer>();

// ad-hoc
var hi = await renderer.RenderAsync("Hi {{ user.name }}!", new { User = new { Name = "squid" } });

// named (registered manually or auto-loaded from templates/)
renderer.Register("welcome", "Welcome {{ user.name }}");
var welcome = await renderer.RenderByNameAsync("welcome", new { User = new { Name = "squid" } });
```

## Key types

| Type | Purpose |
|------|---------|
| `ITemplateRenderer` | Render ad-hoc and named templates. |
| `ScribanTemplateRenderer` | Scriban implementation; compiles/caches named templates and auto-loads `templates/*.tmpl`. |
| `TemplateException` | Raised on parse/render failures. |
| `TemplatingRegistrationExtensions` | `AddTemplating(...)` registration. |

## License

MIT — part of [SquidStd](https://github.com/tgiachi/squid-std).
