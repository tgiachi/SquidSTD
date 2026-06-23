using System.Linq.Expressions;
using System.Reflection;

namespace SquidStd.Search.Elasticsearch.Linq;

/// <summary>
/// LINQ surface for the Elasticsearch provider. <see cref="Match{T}" /> and <see cref="FullText{T}" /> are
/// markers recognized by the translator (no standalone runtime behavior); the async terminals execute the query.
/// </summary>
public static class ElasticQueryableExtensions
{
    /// <summary>Full-text match of <paramref name="text" /> against a single field.</summary>
    public static IQueryable<T> Match<T>(this IQueryable<T> source, string field, string text)
        => source.Provider.CreateQuery<T>(
            Expression.Call(
                ((MethodInfo)MethodBase.GetCurrentMethod()!).MakeGenericMethod(typeof(T)),
                source.Expression,
                Expression.Constant(field),
                Expression.Constant(text)));

    /// <summary>Full-text match of <paramref name="text" /> across all fields.</summary>
    public static IQueryable<T> FullText<T>(this IQueryable<T> source, string text)
        => source.Provider.CreateQuery<T>(
            Expression.Call(
                ((MethodInfo)MethodBase.GetCurrentMethod()!).MakeGenericMethod(typeof(T)),
                source.Expression,
                Expression.Constant(text)));

    /// <summary>Executes the query and returns all matching documents.</summary>
    public static Task<List<T>> ToListAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default)
        => Provider(source).ToListAsync<T>(source.Expression, cancellationToken);

    /// <summary>Executes a count of matching documents.</summary>
    public static Task<long> CountAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default)
        => Provider(source).CountAsync(source.Expression, cancellationToken);

    /// <summary>Executes the query and returns the first matching document, or null.</summary>
    public static async Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default)
    {
        var limited = source.Take(1);
        var results = await Provider(limited).ToListAsync<T>(limited.Expression, cancellationToken);

        return results.Count > 0 ? results[0] : default;
    }

    private static ElasticQueryProvider Provider<T>(IQueryable<T> source)
        => source.Provider as ElasticQueryProvider
            ?? throw new NotSupportedException("These async terminals require a query created by ISearchService.Query<T>().");
}
