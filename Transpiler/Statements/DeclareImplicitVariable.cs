namespace Ronin.Transpiler.Statements;

internal class DeclareImplicitVariable : Statement
{
    public DeclareImplicitVariable(ref ReadOnlySpan<Token> tokens, Parser parser) : base(tokens[0])
    {
        if (tokens.Length < 5) throw new Parser.Exception($"unexpected end of statement {string.Join(' ', tokens.ToArray().Select(token => token.Value))}");
        
        var assign = tokens.IndexOf(Syntax.Assign);
        if (assign is -1) throw new Parser.Exception("expected assignment operator");
        name = tokens[1..assign].ToArray();
        
        var terminal = tokens.IndexOf(Syntax.Terminal);
        if (terminal is -1) throw new Parser.Exception("unexpected end of file");
        var subtokens = tokens[assign..terminal][1..];
        var initializer = parser.Parse(subtokens);
        if (initializer.Length is 0) throw new Parser.Exception("expected initializer");
        if (initializer.Length is not 1) throw new Parser.Exception("expected single initialization statement");
        value = initializer[0];

        tokens = tokens[terminal..][1..];
    }

    public override string ToString() => $"{new string(' ', start.Column)}var {string.Join<Token>('_', name)} = {value};";

    private readonly Token[] name;
    private readonly Statement value;
}
