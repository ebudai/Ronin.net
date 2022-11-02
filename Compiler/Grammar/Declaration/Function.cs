using Ronin.Compiler;

namespace Ronin.Grammar.Declaration;

internal class Function : Syntax, IParsable
{
    internal Modifiers Is { get; private init; }
    internal Identifier Identifier { get; private init; }
    internal Scope Body { get; private init; }

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        var modifiers = Modifiers.Parse(ref parser) as Modifiers;

        var declarator = Declarator.Parse(ref parser);
        if (declarator is Error or null) return declarator;
        if (declarator.Tokens.IsEmpty) return null;
        if (declarator.Tokens.Span[0] is not Lexicon.Reserved.Function) return null;

        var identifier = Identifier.Parse(ref parser);
        if (identifier is Error or null) return identifier;

        var body = Scope.Parse(ref parser);
        if (body is Error or null) return body;

        return new Function
        {
            Is = modifiers,
            Identifier = identifier as Identifier,
            Body = body as Scope,
            Tokens = parser.GetTokens(ref context),
        };
    }
}
