namespace Ronin.Transpiler.Statements;

internal class Literal : Statement
{
    public Literal(ref ReadOnlySpan<Token> tokens) : base(tokens[0])
    {
        value = tokens[0];
        tokens = tokens[1..];
    }

    public override string ToString() => value.Value;

    private readonly Token value;
}
