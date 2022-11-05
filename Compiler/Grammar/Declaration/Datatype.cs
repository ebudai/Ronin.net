using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Declaration;

internal class Datatype : Syntax, IParsable
{
    internal Modifiers Is { get; private init; }
    internal Identifier Identifier { get; private init; }
    internal Reference Algebra { get; private init; }
    internal Scope Body { get; private init; }

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;

        var modifiers = Modifiers.Parse(ref parser) as Modifiers;

        var declarator = Declarator.Parse(ref parser) as Declarator;
        if (declarator is null) return declarator;
        if (declarator.IsDatatype is not true) return null;

        var identifier = Identifier.Parse(ref parser);
        if (identifier is Error or null) return identifier;

        Syntax algebra = null;
        if (parser[0] is Assign)
        {
            ++parser.Cursor;
            algebra = Reference.Parse(ref parser);
            if (algebra is Error or null) return algebra;
        }

        var body = Scope.Parse(ref parser);
        if (body is Error or null) return body;

        return new Datatype
        {
            Is = modifiers,
            Identifier = identifier as Identifier,
            Algebra = algebra as Reference,
            Body = body as Scope,
            Tokens = parser.GetTokens(ref context)
        };
    }
}
