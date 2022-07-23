namespace Ronin.Transpiler.Statements;

internal class Identifier : Statement
{
    public Identifier(ref ReadOnlySpan<Token> tokens) : base(tokens[0])
    {
        int i = 0;
        for (int max = tokens.Length; i != max; ++i)
        {
            if (tokens[i].Kind is not Token.Type.Identifier) break;
        }
        value = tokens[..i].ToArray();
        tokens = tokens[i..];
    }

    public override string ToString() => string.Join(' ', value.Select(token => token.Value));

    private readonly Token[] value;
}
