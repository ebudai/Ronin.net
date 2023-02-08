using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Interval : Syntax, Compiler.IParsable<Interval>
{
    public Value Start { get; init; }
    public Value End { get; init; }
    
    public static Interval Parse(ref Parser context)
    {
        Parser parser = context;

        if (Value.Parse(ref parser) is not Value start) return null;

        if (parser.CurrentToken is not Lexicon.Symbols.Range) return null;
        parser.Advance();

        if (Value.Parse(ref parser) is not Value end) return null;

        return new Interval
        {
            Start = start,
            End = end,
            Source = parser.Commit(ref context)
        };
    }
}
