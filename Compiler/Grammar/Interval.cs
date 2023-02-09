using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Interval : Syntax, Compiler.IParsable<Interval>
{
    public Value Start { get; init; }
    public Value End { get; init; }
    
    public static Interval Parse(ref Parser context)
    {
        Parser parser = context;

        var start = Value.Parse(ref parser);

        if (parser.FailedToConsume<Lexicon.Symbols.Range>()) return null;

        var end = Value.Parse(ref parser);

        return new Interval
        {
            Start = start,
            End = end,
            Source = parser.Commit(ref context)
        };
    }
}
