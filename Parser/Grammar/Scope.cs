using Ronin.Parser.Grammar.Aggregates;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Parser.Grammar;

internal class Scope : Aggregate
{
    public Scope() { }

    [ExcludeFromCodeCoverage]
    public override string ToString() => "{ ... }";

    internal bool TryAdd(Expression expression)
    {
        if (expression is null) return false;
        if (!expression.IsEmpty) Expressions.Add(expression);
        return !expression.IsScopeClose;
    }
}
