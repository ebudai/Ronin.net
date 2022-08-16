using Ronin.Grammar;

namespace Ronin.Parser;

internal static class ScopeParser
{
    internal static Scope Parse(Context context)
    {
        var symbol = SymbolParser.Parse(context);
        if (symbol is not OpeningBrace)
        {
            context.Retreat(symbol?.Value.Length ?? 0);
            return null;
        }

        Scope scope = new();

        while (scope.TryAdd(ExpressionParser.Parse(context))) { }

        return scope;
    }

    internal static bool TryAdd(this Scope @this, Expression expression)
    {
        if (expression is null) return false;
        if (!expression.IsEmpty) @this.Expressions.Add(expression);
        return !expression.IsScopeClose;
    }
}
