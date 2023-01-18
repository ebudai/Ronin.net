using Ronin.Compiler;

using Object = Ronin.Grammar.Aggregates.Object;

namespace Ronin.Grammar;

internal class Algebra : Syntax, IParsable
{
    public Syntax Syntax { get; init; }

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = Scalar.Parse(ref parser)
            ?? Object.Parse(ref parser)
            ?? Name.Parse(ref parser);

        if (syntax is Error or null) return syntax;

        return new Algebra { Syntax = syntax, Source = parser.Commit(ref context) };
    }
}
