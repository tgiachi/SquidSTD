# Search: index and query with Elasticsearch

Index typed documents and query them with LINQ, translated to the Elasticsearch query DSL.

## What you'll build

A host using `SquidStd.Search.Elasticsearch`: register the provider, mark a record as indexable, then index a
document and query it back with a strongly-typed `IQueryable<T>`. The contracts live in
`SquidStd.Search.Abstractions`, so your code stays decoupled from the Elasticsearch client.

## Prerequisites

- .NET 10 SDK
- `dotnet add package SquidStd.Search.Elasticsearch`
- A running Elasticsearch (only needed to actually execute queries):

  ```bash
  docker run -p 9200:9200 -e discovery.type=single-node -e xpack.security.enabled=false elasticsearch:8.6.1
  ```

## Steps

### 1. Register the Elasticsearch provider

`AddElasticsearch` registers the client and `ISearchService` against your cluster endpoint.

[!code-csharp[](../../samples/SquidStd.Samples.Search/Program.cs#step-1)]

### 2. Define an indexable document

Implement `IIndexableEntity` (supplying the document id) and tag the type with `[SearchIndex("orders")]`.

[!code-csharp[](../../samples/SquidStd.Samples.Search/Program.cs#step-2)]

### 3. Index and query

`IndexAsync` stores a document; `Query<T>()` returns a LINQ surface whose async terminals
(`ToListAsync`, `CountAsync`, `FirstOrDefaultAsync`) execute against the cluster.

[!code-csharp[](../../samples/SquidStd.Samples.Search/Program.cs#step-3)]

## Run it

```bash
dotnet run --project samples/SquidStd.Samples.Search
```

With Elasticsearch running it prints `found 1 open order(s)`.

## How it works

`ISearchService.Query<T>()` returns a constrained `IQueryable<T>` backed by a custom provider. The expression
tree is translated to the Elasticsearch query DSL by `ElasticExpressionTranslator`; the async terminals and the
`.Match`/`.FullText` markers come from `SquidStd.Search.Elasticsearch.Linq`. The target index is resolved from
the `[SearchIndex]` attribute (with `${VAR}` expansion) or the lowercased type name.

## See also

- [SquidStd.Search.Elasticsearch reference](../articles/search-elasticsearch.md)
- [SquidStd.Search.Abstractions reference](../articles/search-abstractions.md)
