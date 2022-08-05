namespace Ronin.Parser;

internal class Scope : Aggregate
{
    public Scope() { }

    public override string ToString() => "{ ... }";

    internal bool TryAdd(Expression expression)
    {
        if (expression is null) return false;
        if (!expression.IsEmpty) Expressions.Add(expression);
        return !expression.IsScopeClose;
    }
}
