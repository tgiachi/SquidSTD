using ZLinq;

namespace SquidStd.Database.Extensions;

/// <summary>
/// Zero-allocation, in-memory helpers (ZLinq) over already-materialized result lists.
/// </summary>
public static class ZLinqResultExtensions
{
    /// <summary>
    /// Projects each materialized item to a new form using ZLinq, returning a list.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The projected item type.</typeparam>
    /// <param name="source">The materialized source items.</param>
    /// <param name="selector">The projection.</param>
    /// <returns>The projected list.</returns>
    public static List<TResult> MapToList<TSource, TResult>(
        this IReadOnlyList<TSource> source,
        Func<TSource, TResult> selector)
    {
        return source.AsValueEnumerable().Select(selector).ToList();
    }

    /// <summary>
    /// Takes an in-memory page of a materialized list using ZLinq (no SQL involved).
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The materialized source items.</param>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>The page items.</returns>
    public static List<T> PageInMemory<T>(this IReadOnlyList<T> source, int page, int pageSize)
    {
        var skip = Math.Max(0, (page - 1) * pageSize);

        return source.AsValueEnumerable().Skip(skip).Take(pageSize).ToList();
    }
}
