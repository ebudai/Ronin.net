namespace Ronin.Parser.Grammar.Aggregates;

internal class Aggregate : Syntax
{
    internal List<Expression> Expressions { get; } = new();
}
