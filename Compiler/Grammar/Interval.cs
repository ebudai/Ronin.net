// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Interval : Anonymous, IParsableSyntax<Interval>
{
    public Component Start { get; init; }
    public Component End { get; init; }
    
    public new static Interval Parse(ref Parser current)
    {
        Parser parser = current;

        var start = Component.Parse(ref parser);

        if (parser.TryConsume<Lexicon.Punctuation.Range>() is false) return null;

        var end = Component.Parse(ref parser);

        if (start is null && end is null) return null;

        return new Interval
        {
            Start = start,
            End = end,
            Source = parser.Commit(ref current)
        };
    }

    public class Component : CompositeSyntax<Component, Literal, Name> { }
}
