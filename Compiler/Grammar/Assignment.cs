// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     Sets the current <see cref="Grammar.Value"/> of a <see cref="Datum"/>
/// </summary>
/// 
/// <example>
///     x = 16;
/// </example>
internal class Assignment : Statement, IParsableSyntax<Assignment>
{
    public Datum Destination { get; set; }
    public Punctuation Type { get; init; }
    public Value Value { get; set; }

    public new static Assignment Parse(ref Parser current)
    {
        Parser parser = current;

        if (Reference.Parse(ref parser) is not Reference reference) return null;

        if (parser.Token is not Assign) return null;

        var type = parser.Token as Punctuation;
        parser.Advance();

        if (Value.Parse(ref parser) is not Value value) return null;

        return new Assignment
        {
            Destination = new Datum.Unresolved { Reference = reference },
            Type = type,
            Value = value,
            Source = parser.Commit(ref current),
        };
    }
}