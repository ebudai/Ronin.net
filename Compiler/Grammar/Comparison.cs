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
internal class Comparison : Statement, IParsableSyntax<Comparison>
{
    public Datum Destination { get; set; }
    public Assign Operation { get; init; }
    public Value Value { get; set; }

    public new static Comparison Parse(ref Parser current)
    {
        Parser parser = current;

        if (Reference.Parse(ref parser) is not Reference reference) return null;

        if (parser.Token is not Assign operation) return null;
        parser.Advance();

        if (Value.Parse(ref parser) is not Value value) return null;

        return new Comparison
        {
            Destination = new Datum.Unresolved { Reference = reference },
            Operation = operation,
            Value = value,
            Source = parser.Commit(ref current),
        };
    }
}