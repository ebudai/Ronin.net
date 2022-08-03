namespace Ronin.Parser;

internal class Aggregate : Syntax
{
    internal List<Expression> Expressions { get; } = new();
}
