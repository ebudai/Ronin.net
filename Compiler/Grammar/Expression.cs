namespace Ronin.Grammar;

internal class Expression : Syntax
{
    internal List<Expression> Children { get; } = new();
}
