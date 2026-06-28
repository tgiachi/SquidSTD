using System.Linq.Expressions;
using System.Reflection;

namespace SquidStd.Search.Elasticsearch.Linq;

/// <summary>
///     LINQ surface for the Elasticsearch provider. <see cref="Match{T}" /> and <see cref="FullText{T}" /> are
///     markers recognized by the translator (no standalone runtime behavior); the async terminals execute the query.
/// </summary>
public static class ElasticQueryableExtensions
{
    private static ElasticQueryProvider Provider<T>(IQueryable<T> source)
    {
        return source.Provider as ElasticQueryProvider ??
               throw new NotSupportedException(
                   "These async terminals require a query created by ISearchService.Query<T>()."
               );
    }

    extension<T>(IQueryable<T> source)
    {
        /// <summary>Executes a count of matching documents.</summary>
        public Task<long> CountAsync(CancellationToken cancellationToken = default)
        {
            return Provider(source).CountAsync(source.Expression, cancellationToken);
        }

        /// <summary>Executes the query and returns the first matching document, or null.</summary>
        public async Task<T?> FirstOrDefaultAsync(
            CancellationToken cancellationToken = default
        )
        {
            var limited = source.Take(1);
            var results = await Provider(limited).ToListAsync<T>(limited.Expression, cancellationToken);

            return results.Count > 0 ? results[0] : default;
        }

        /// <summary>Full-text match of <paramref name="text" /> across all fields.</summary>
        public IQueryable<T> FullText(string text)
        {
            return source.Provider.CreateQuery<T>(
                Expression.Call(
                    ((MethodInfo)MethodBase.GetCurrentMethod()!).MakeGenericMethod(typeof(T)),
                    source.Expression,
                    Expression.Constant(text)
                )
            );
        }

        /// <summary>Full-text match of <paramref name="text" /> against a single field.</summary>
        public IQueryable<T> Match(string field, string text)
        {
            return source.Provider.CreateQuery<T>(
                Expression.Call(
                    ((MethodInfo)MethodBase.GetCurrentMethod()!).MakeGenericMethod(typeof(T)),
                    source.Expression,
                    Expression.Constant(field),
                    Expression.Constant(text)
                )
            );
        }

        /// <summary>Executes the query and returns all matching documents.</summary>
        public Task<List<T>> ToListAsync(CancellationToken cancellationToken = default)
        {
            return Provider(source).ToListAsync<T>(source.Expression, cancellationToken);
        }
    }
}
