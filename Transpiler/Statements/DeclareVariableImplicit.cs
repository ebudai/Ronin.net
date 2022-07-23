namespace Ronin.Transpiler.Statements;

// form for implicitly typed variable declarations is
//      "var" identifier[name] = statement{value}.
internal class DeclareVariableImplicit : Statement
{
    public string Name => string.Join<Token>(' ', Names);

    public readonly Token[] Names;
    public readonly Statement Statement;

    public DeclareVariableImplicit(ref ReadOnlySpan<Token> tokens, Parser parser) : base(tokens[0])
    {
        Expect(tokens.Length >= 5, $"unexpected end of file");

        int assign = tokens.IndexOf(Syntax.Assign);
        Expect(assign is not -1, $"expected {Syntax.Assign} for implicit variable declaration");

        Names = tokens[1..assign].ToArray();
        
        var terminal = tokens.IndexOf(Syntax.Terminal);
        Expect(terminal is not -1, "unexpected end of file");

        var subtokens = tokens[++assign..terminal];
        var initializer = parser.Parse(subtokens);
        Expect(initializer.Length is not 0, "expected initializer");
        Expect(initializer.Length is 1, "expected single initialization statement");
        Statement = initializer[0];  

        tokens = tokens[++terminal..];
    }

    public override string ToString()
    {
        string indent = new(' ', start.Column);
        return $"{indent}var {Name} = {Statement};";
    }
}
