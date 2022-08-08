namespace Ronin.Parser.Grammar.Aggregates;

internal class Aggregate : Syntax
{
    protected internal List<Expression> Expressions { get; } = new();
}
