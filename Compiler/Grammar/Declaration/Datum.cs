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
        var parsed = Declarator.Parse(ref parser);
        if (parsed is Error or null) return parsed;
        var declarator = parsed as Declarator;
        if (declarator.Variable || declarator.Constant || declarator.Reactive)
        {
            var syntax = Parameter.Parse(ref parser);
            if (syntax is Error or null) return syntax;
            var parameter = syntax as Parameter;

            return new Datum
            {
                Is = parsed as Declarator,
                Name = parameter.Name,
                Modifiers = parameter.Is,
                Datatype = parameter.Datatype,
                Initializer = parameter.Initializer,
                Tokens = parser.GetTokens(ref context)
            };
        }
        return null;
    }
}