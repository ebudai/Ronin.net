using Ronin.Compiler;

using Arguments = Ronin.Grammar.Aggregates.Arguments;

namespace Ronin.Grammar;

internal class Algebra : Syntax, IParsable
{
    public Syntax Syntax { get; init; }

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = Scalar.Parse(ref parser)
            ?? Arguments.Parse(ref parser)
            ?? Name.Parse(ref parser);

        if (syntax is Error or null) return syntax;

        return new Algebra { Syntax = syntax, Source = parser.Commit(ref context) };
    }
}
