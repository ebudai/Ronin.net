namespace Ronin.Transpiler.Statements;

internal class DeclareVariable : Statement
{
    public DeclareVariable(ref ReadOnlySpan<Token> tokens, Parser parser) : base(tokens[0])
    {
        if (tokens.Length < 5) throw new Parser.Exception($"unexpected end of statement {string.Join(' ', tokens.ToArray().Select(token => token.Value))}");

        int assign = tokens.IndexOf(Syntax.Assign);
        var typestart = tokens.IndexOf(Syntax.TypeDeclareStart) + 1;
        if (typestart is 0)
        {
            // implicitly typed var
            if (assign is -1) throw new Parser.Exception($"expected {Syntax.Assign} for implicit variable declaration");
            name = tokens[1..assign].ToArray();
        }
        else
        {
            // explictly typed var
            var typeend = tokens.IndexOf(Syntax.TypeDeclareEnd);
            if (typeend is -1) throw new Parser.Exception($"expected {Syntax.TypeDeclareEnd}");
            type = tokens[typestart..typeend].ToArray();
        }
        
        var terminal = tokens.IndexOf(Syntax.Terminal);
        if (terminal is -1) throw new Parser.Exception("unexpected end of file");

        if (assign is not -1)
        {
            var subtokens = tokens[assign..terminal][1..];
            var initializer = parser.Parse(subtokens);
            if (initializer.Length is 0) throw new Parser.Exception("expected initializer");
            if (initializer.Length is not 1) throw new Parser.Exception("expected single initialization statement");
            value = initializer[0];
        }        

        tokens = tokens[terminal..][1..];
    }

    public override string ToString() => $"{new string(' ', start.Column)}var {string.Join<Token>('_', name)} = {value};";

    private readonly Token[] type;
    private readonly Token[] name;
    private readonly Statement value;
}
