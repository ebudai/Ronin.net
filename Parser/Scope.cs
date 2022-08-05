namespace Ronin.Parser;

internal class Scope : Aggregate
{
    public Scope() { }

    public override string ToString() => "{ ... }";

    internal bool Add(Expression expression, ref int _)
    {
        if (expression.IsEmpty) return false;
        Expressions.Add(expression);
        return true;
    }

    internal bool Add(Expression expression)
    {
        if (expression is null) return false;
        Expressions.Add(expression);
        return true;
    }
}
