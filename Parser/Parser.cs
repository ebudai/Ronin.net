using Ronin.Grammar;

namespace Ronin.Parser;

public static class Parser
{
    public static Syntax Parse(Context context)
    {
        context.Lex(Form.whitespace);

        if (context.IsAtEnd) return null;

        return LiteralParser.Parse(context)
            ?? ParametersParser.Parse(context)
            ?? AggregateParser.Parse(context)
            ?? ScopeParser.Parse(context)
            ?? SymbolParser.Parse(context)
            ?? DeclarationParser.Parse(context)
            ?? IdentifierParser.Parse(context) as Syntax;
    }
}
