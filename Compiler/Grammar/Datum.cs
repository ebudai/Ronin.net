using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Reserved;

namespace Ronin.Grammar;

internal class Datum : Parameter, IParsable
{
    internal Keyword Mutability { get; private init; }

    public static new Syntax Parse(ref Parser context)
    {
        Keyword declarator = context.Current is Variable or Constant or Reactive ? context.Current as Keyword : null;
        if (declarator is null) return null;

        Parser parser = context;
        parser.Advance();

        var syntax = Parameter.Parse(ref parser);
        if (syntax is Error or null) return syntax;
        var parameter = syntax as Parameter;

        return new Datum
        {
            Mutability = declarator,
            Name = parameter.Name,
            Is = parameter.Is,
            Datatype = parameter.Datatype,
            Initializer = parameter.Initializer,
            Source = parser.Commit(ref context)
        };
    }
}