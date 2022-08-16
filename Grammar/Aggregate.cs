namespace Ronin.Grammar;

public class Aggregate : Syntax
{
    public List<Expression> Expressions { get; } = new();
}
