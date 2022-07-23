namespace Ronin.Transpiler.Statements;

// form for explicitly typed variable declarations is
//      "var" identifier[name] (identifier[type]).
//   or "var" identifier[name] (identifier[type]) = statement{value}.
internal class DeclareVariableExplicit : Statement
{
    public string Type => string.Join<Token>(' ', Types);
    public string Name => string.Join<Token>(' ', Names);

    public readonly Token[] Types;
    public readonly Token[] Names;
    public readonly Statement Statement = null;

    public DeclareVariableExplicit(ref ReadOnlySpan<Token> tokens, Parser parser) : base(tokens[0])
    {
        Expect(tokens.Length >= 6, $"unexpected end of file");

        var typestart = tokens.IndexOf(Syntax.DeclareVariableTypeStart);
        Expect(typestart is not -1, $"exptected {Syntax.DeclareVariableTypeStart}");

        Names = tokens[1..typestart].ToArray();

        var typeend = tokens.IndexOf(Syntax.DeclareVariableTypeEnd);
        Expect(typeend is not -1, $"expected {Syntax.DeclareVariableTypeEnd}");
        
        Types = tokens[++typestart..typeend].ToArray();

        var terminal = tokens.IndexOf(Syntax.Terminal);
        Expect(terminal is not -1, "unexpected end of file");

        int assign = tokens.IndexOf(Syntax.Assign);
        if (assign is not -1 && assign < terminal)
        {
            var subtokens = tokens[++assign..terminal];
            var initializer = parser.Parse(subtokens);
            Expect(initializer.Length is not 0, "expected initializer");
            Expect(initializer.Length is 1, "expected single initialization statement");
            Statement = initializer[0];
        }

        tokens = tokens[++terminal..];
    }

    public override string ToString()
    {
        string indent = new(' ', start.Column);
        var declaration = $"{indent}{Type} {Name}";
        var initializer = Statement is null ? string.Empty : $" = {Statement}";
        return $"{declaration}{initializer};";
    }
}
