using System.Linq.Expressions;
using SquidStd.Search.Abstractions.Interfaces;
using SquidStd.Search.Elasticsearch.Linq;

namespace SquidStd.Tests.Search;

public class ElasticExpressionTranslatorTests
{
    private sealed record Doc(string Status, int Total, string Name) : IIndexableEntity
    {
        public string IndexId => Name;
    }

    private static ElasticQuery Translate(Func<IQueryable<Doc>, IQueryable<Doc>> build)
    {
        IQueryable<Doc> root = new TranslateOnlyQueryable<Doc>();
        var query = build(root);

        return ElasticExpressionTranslator.Translate(query.Expression, typeof(Doc));
    }

    [Fact]
    public void Where_Equality_ProducesTermOnKeyword()
    {
        var q = Translate(s => s.Where(d => d.Status == "open"));

        var term = q.Query["bool"]!["must"]!.AsArray()[0]!["term"]!.AsObject();
        Assert.True(term.ContainsKey("status.keyword"));
        Assert.Equal("open", term["status.keyword"]!.GetValue<string>());
    }

    [Fact]
    public void Where_Range_And_Order_And_Take()
    {
        var q = Translate(s => s.Where(d => d.Total > 100).OrderByDescending(d => d.Total).Take(5));

        Assert.Equal(5, q.Size);
        Assert.Equal("desc", q.Sort!.AsArray()[0]!["total"]!["order"]!.GetValue<string>());
        var range = q.Query["bool"]!["must"]!.AsArray()[0]!["range"]!["total"]!.AsObject();
        Assert.Equal(100, range["gt"]!.GetValue<int>());
    }

    [Fact]
    public void Match_ProducesMatchClause()
    {
        var q = Translate(s => s.Match("name", "laptop"));

        var match = q.Query["bool"]!["must"]!.AsArray()[0]!["match"]!.AsObject();
        Assert.Equal("laptop", match["name"]!.GetValue<string>());
    }

    [Fact]
    public void UnsupportedExpression_Throws()
    {
        Assert.Throws<NotSupportedException>(() => Translate(s => s.Where(d => d.Name.ToUpperInvariant() == "X")));
    }

    private sealed class TranslateOnlyQueryable<T> : IOrderedQueryable<T>, IQueryProvider
    {
        public TranslateOnlyQueryable()
        {
            Expression = Expression.Constant(this);
        }

        private TranslateOnlyQueryable(Expression expression)
        {
            Expression = expression;
        }

        public Type ElementType => typeof(T);
        public Expression Expression { get; }
        public IQueryProvider Provider => this;

        public IQueryable CreateQuery(Expression expression) => new TranslateOnlyQueryable<T>(expression);
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TranslateOnlyQueryable<TElement>(expression);
        public object? Execute(Expression expression) => throw new NotSupportedException();
        public TResult Execute<TResult>(Expression expression) => throw new NotSupportedException();
        public IEnumerator<T> GetEnumerator() => throw new NotSupportedException();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw new NotSupportedException();
    }
}
