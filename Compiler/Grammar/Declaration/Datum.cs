using Ronin.Compiler;
using Ronin.Lexicon.Reserved;

namespace Ronin.Grammar.Declaration;

internal class Datum : Syntax, IParsable
{
    internal Declarator Is { get; private init; }
    internal string Name { get; private init; }
    internal Modifiers Modifiers { get; private init; }
    internal Reference Datatype { get; private init; }
    internal Value Initializer { get; private init; }

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        var declarator = Declarator.Parse(ref parser);
        if (declarator is Error or null) return declarator;
        if (declarator.Tokens.IsEmpty) return null;
        if (declarator.Tokens.Span[0] is not Variable and not Constant and not Reactive) return null;

        var modifiers = Modifiers.Parse(ref parser) as Modifiers;

        var syntax = Parameter.Parse(ref parser);
        if (syntax is Error or null) return syntax;
        var parameter = syntax as Parameter;

        return new Datum
        {
            Is = declarator as Declarator,
            Name = parameter.Name,
            Modifiers = modifiers,
            Datatype = parameter.Datatype,
            Initializer = parameter.Initializer,
            Tokens = parser.GetTokens(ref context)
        };
    }
}