using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SquidStd.Search.Elasticsearch.Linq;

/// <summary>
/// Translates a constrained LINQ expression chain into an <see cref="ElasticQuery" />. Supported: Where
/// (==, !=, &lt;, &gt;, &lt;=, &gt;=, &amp;&amp;, ||, !, bool members, string.Contains/StartsWith),
/// OrderBy/ThenBy(Descending), Skip, Take, and the Match/FullText markers. Anything else throws
/// <see cref="NotSupportedException" />.
/// </summary>
public static class ElasticExpressionTranslator
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Translates the query expression for an element type.</summary>
    public static ElasticQuery Translate(Expression expression, Type elementType)
    {
        var result = new ElasticQuery();
        var must = new JsonArray();
        var sort = new JsonArray();

        Walk(expression, must, sort, result);

        if (must.Count > 0)
        {
            result.Query = new JsonObject { ["bool"] = new JsonObject { ["must"] = must } };
        }

        if (sort.Count > 0)
        {
            result.Sort = sort;
        }

        return result;
    }

    private static void Walk(Expression expression, JsonArray must, JsonArray sort, ElasticQuery result)
    {
        if (expression is not MethodCallExpression call)
        {
            return;
        }

        Walk(call.Arguments[0], must, sort, result);

        switch (call.Method.Name)
        {
            case "Where":
                must.Add(TranslatePredicate(UnquoteLambda(call.Arguments[1]).Body));
                break;
            case "OrderBy":
            case "ThenBy":
                sort.Add(SortClause(call.Arguments[1], "asc"));
                break;
            case "OrderByDescending":
            case "ThenByDescending":
                sort.Add(SortClause(call.Arguments[1], "desc"));
                break;
            case "Skip":
                result.From = (int)GetConstant(call.Arguments[1])!;
                break;
            case "Take":
                result.Size = (int)GetConstant(call.Arguments[1])!;
                break;
            case "Match":
                must.Add(new JsonObject
                {
                    ["match"] = new JsonObject
                    {
                        [(string)GetConstant(call.Arguments[1])!] = JsonValue.Create((string)GetConstant(call.Arguments[2])!)
                    }
                });
                break;
            case "FullText":
                must.Add(new JsonObject
                {
                    ["query_string"] = new JsonObject { ["query"] = JsonValue.Create((string)GetConstant(call.Arguments[1])!) }
                });
                break;
            default:
                throw new NotSupportedException(
                    $"LINQ operator '{call.Method.Name}' is not supported by the Elasticsearch provider. Use the native ElasticsearchClient for advanced queries.");
        }
    }

    private static JsonObject TranslatePredicate(Expression expression)
    {
        switch (expression)
        {
            case BinaryExpression { NodeType: ExpressionType.AndAlso } and1:
                return new JsonObject
                {
                    ["bool"] = new JsonObject { ["must"] = new JsonArray(TranslatePredicate(and1.Left), TranslatePredicate(and1.Right)) }
                };
            case BinaryExpression { NodeType: ExpressionType.OrElse } or1:
                return new JsonObject
                {
                    ["bool"] = new JsonObject
                    {
                        ["should"] = new JsonArray(TranslatePredicate(or1.Left), TranslatePredicate(or1.Right)),
                        ["minimum_should_match"] = 1
                    }
                };
            case UnaryExpression { NodeType: ExpressionType.Not } not:
                return new JsonObject { ["bool"] = new JsonObject { ["must_not"] = new JsonArray(TranslatePredicate(not.Operand)) } };
            case BinaryExpression binary:
                return TranslateComparison(binary);
            case MemberExpression member when member.Type == typeof(bool):
                return new JsonObject { ["term"] = new JsonObject { [FieldName(member)] = true } };
            case MethodCallExpression methodCall:
                return TranslateStringMethod(methodCall);
            default:
                throw Unsupported(expression);
        }
    }

    private static JsonObject TranslateComparison(BinaryExpression binary)
    {
        var (member, valueExpr) = OrientMemberValue(binary);
        var field = FieldName(member);
        var value = ToJsonValue(EvaluateValue(valueExpr));

        return binary.NodeType switch
        {
            ExpressionType.Equal => TermOrKeyword(field, member.Type, value),
            ExpressionType.NotEqual => new JsonObject { ["bool"] = new JsonObject { ["must_not"] = new JsonArray(TermOrKeyword(field, member.Type, value)) } },
            ExpressionType.GreaterThan => Range(field, "gt", value),
            ExpressionType.GreaterThanOrEqual => Range(field, "gte", value),
            ExpressionType.LessThan => Range(field, "lt", value),
            ExpressionType.LessThanOrEqual => Range(field, "lte", value),
            _ => throw Unsupported(binary)
        };
    }

    private static JsonObject TranslateStringMethod(MethodCallExpression call)
    {
        if (call.Object is not MemberExpression member)
        {
            throw Unsupported(call);
        }

        var field = FieldName(member);
        var arg = (string)EvaluateValue(call.Arguments[0])!;

        return call.Method.Name switch
        {
            "Contains" => new JsonObject { ["wildcard"] = new JsonObject { [$"{field}.keyword"] = $"*{arg}*" } },
            "StartsWith" => new JsonObject { ["prefix"] = new JsonObject { [$"{field}.keyword"] = arg } },
            _ => throw Unsupported(call)
        };
    }

    private static JsonObject TermOrKeyword(string field, Type memberType, JsonNode? value)
    {
        var termField = memberType == typeof(string) ? $"{field}.keyword" : field;

        return new JsonObject { ["term"] = new JsonObject { [termField] = value } };
    }

    private static JsonObject Range(string field, string op, JsonNode? value)
        => new() { ["range"] = new JsonObject { [field] = new JsonObject { [op] = value } } };

    private static JsonObject SortClause(Expression keySelector, string order)
    {
        var member = (MemberExpression)UnquoteLambda(keySelector).Body;

        return new JsonObject { [FieldName(member)] = new JsonObject { ["order"] = order } };
    }

    private static (MemberExpression Member, Expression Value) OrientMemberValue(BinaryExpression binary)
    {
        if (StripConvert(binary.Left) is MemberExpression leftMember && IsParameterBound(leftMember))
        {
            return (leftMember, binary.Right);
        }

        if (StripConvert(binary.Right) is MemberExpression rightMember && IsParameterBound(rightMember))
        {
            return (rightMember, binary.Left);
        }

        throw Unsupported(binary);
    }

    private static bool IsParameterBound(Expression expression)
        => expression switch
        {
            ParameterExpression => true,
            MemberExpression m => m.Expression is not null && IsParameterBound(m.Expression),
            _ => false
        };

    private static Expression StripConvert(Expression expression)
        => expression is UnaryExpression { NodeType: ExpressionType.Convert } convert ? convert.Operand : expression;

    private static string FieldName(MemberExpression member)
    {
        var parts = new Stack<string>();
        Expression? current = member;

        while (current is MemberExpression m)
        {
            parts.Push(JsonNamingPolicy.CamelCase.ConvertName(m.Member.Name));
            current = m.Expression;
        }

        return string.Join('.', parts);
    }

    private static object? EvaluateValue(Expression expression)
        => expression is ConstantExpression constant
            ? constant.Value
            : Expression.Lambda(expression).Compile().DynamicInvoke();

    private static object? GetConstant(Expression expression)
        => EvaluateValue(expression);

    private static JsonNode? ToJsonValue(object? value)
        => value is null ? null : JsonSerializer.SerializeToNode(value, WebOptions);

    private static LambdaExpression UnquoteLambda(Expression expression)
        => expression is UnaryExpression { NodeType: ExpressionType.Quote } quote
            ? (LambdaExpression)quote.Operand
            : (LambdaExpression)expression;

    private static NotSupportedException Unsupported(Expression expression)
        => new($"Expression '{expression}' is not supported by the Elasticsearch provider. Use the native ElasticsearchClient for advanced queries.");
}
