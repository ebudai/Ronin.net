// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Interval : Syntax, IParsableSyntax<Interval>
{
    public Value Start { get; init; }
    public Value End { get; init; }
    
    public static Interval Parse(ref Parser current)
    {
        Parser parser = current;

        var start = Value.Parse(ref parser);

        if (parser.TryConsume<Lexicon.Punctuation.Range>() is false) return null;

        var end = Value.Parse(ref parser);

        return new Interval
        {
            Start = start,
            End = end,
            Source = parser.Commit(ref current)
        };
    }
}
