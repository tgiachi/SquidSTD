# Templating (Scriban)

Render Scriban templates from inline strings or registered named templates against a model.

## What you'll build

A host that resolves `ITemplateRenderer` (`SquidStd.Templating`), renders an inline template against a model, then
registers a named template and renders it by name.

## Prerequisites

- .NET 10 SDK
- `dotnet add package SquidStd.Templating`
- No external infrastructure required

## Steps

### 1. Register templating

[!code-csharp[](../../samples/SquidStd.Samples.Templating/Program.cs#step-1)]

### 2. Render an inline template

`RenderAsync` compiles and renders an ad-hoc template string against a model; properties are resolved with the
Scriban `{{ ... }}` syntax.

[!code-csharp[](../../samples/SquidStd.Samples.Templating/Program.cs#step-2)]

### 3. Register and render a named template

`Register` compiles and caches a template under a name; `RenderByNameAsync` renders it later without recompiling.

[!code-csharp[](../../samples/SquidStd.Samples.Templating/Program.cs#step-3)]

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.Templating
```

Prints `Hi squid!` then `Welcome aboard, squid.`.

## How it works

`ITemplateRenderer` wraps the Scriban engine: `RenderAsync` handles one-off templates, while `Register` plus
`RenderByNameAsync` lets you compile a template once and reuse it by name. The model is bound by property name using
Scriban's case-insensitive member access.

## See also

- [SquidStd.Templating reference](../articles/templating.md)
