using Ronin.Parser.Grammar.Aggregates;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Parser.Grammar;

internal class Scope : Aggregate
{
    internal Scope() { }

    [ExcludeFromCodeCoverage]
    public override string ToString() => "{ ... }";

    internal bool TryAdd(Expression expression)
    {
        if (expression is null) return false;
        if (!expression.IsEmpty) Expressions.Add(expression);
        return !expression.IsScopeClose;
    }

    internal new static Scope Parse(Context context)
    {
        var symbol = Symbol.Parse(context);
        if (symbol is not OpeningBrace)
        {
            context.Retreat(symbol?.Value.Length ?? 0);
            return null;
        }

        Scope scope = new();

        while (scope.TryAdd(Expression.Parse(context))) { }

        return scope;
    }
}
