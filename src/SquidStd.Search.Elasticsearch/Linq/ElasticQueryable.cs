using System.Collections;
using System.Linq.Expressions;

namespace SquidStd.Search.Elasticsearch.Linq;

/// <summary>An <see cref="IQueryable{T}" /> backed by <see cref="ElasticQueryProvider" />.</summary>
public sealed class ElasticQueryable<T> : IOrderedQueryable<T>
{
    public Type ElementType => typeof(T);

    public Expression Expression { get; }

    public IQueryProvider Provider { get; }

    public ElasticQueryable(ElasticQueryProvider provider, Expression expression)
    {
        Provider = provider;
        Expression = expression;
    }

    public ElasticQueryable(ElasticQueryProvider provider)
    {
        Provider = provider;
        Expression = Expression.Constant(this);
    }

    public IEnumerator<T> GetEnumerator()
    {
        throw new NotSupportedException("Use the async terminals (ToListAsync/CountAsync/FirstOrDefaultAsync).");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
