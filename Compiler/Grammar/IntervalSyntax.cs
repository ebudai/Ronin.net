// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Grammar;

internal class IntervalSyntax : Syntax, Compiler.IParsable<IntervalSyntax>
{
    public Value Start { get; init; }
    public Value End { get; init; }
    
    public static IntervalSyntax Parse(ref Parser context)
    {
        Parser parser = context;

        var start = Value.Parse(ref parser);

        if (parser.FailsToConsume<Lexicon.Symbols.Range>()) return null;

        var end = Value.Parse(ref parser);

        return new IntervalSyntax
        {
            Start = start,
            End = end,
            Source = parser.Commit(ref context)
        };
    }
}
