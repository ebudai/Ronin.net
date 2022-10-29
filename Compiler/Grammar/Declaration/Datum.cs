using Ronin.Compiler;
using Ronin.Lexicon.Reserved;

namespace Ronin.Grammar.Declaration;

internal class Datum : Syntax, IParsable
{
    internal Parameter Parameter { get; private init; }

    public static Syntax Parse(Parser parser)
    {
        var declarator = Declarator.Parse(parser);
        if (declarator is Error or null) return declarator;
        if (declarator.Tokens.IsEmpty) return null;
        if (declarator.Tokens.Span[0] is not Variable or Constant or Reactive) return null;

        parser.AdvancePastTrivia();

        var parameter = Parameter.Parse(parser);
        if (parameter is Error or null) return parameter;

        return new Datum { Parameter = parameter as Parameter, Tokens = parser.Tokens };
    }
}