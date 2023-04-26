// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Grammar;

/// <summary>
///     Sets the current <see cref="Grammar.Value"/> of a <see cref="Datum"/>
/// </summary>
/// 
/// <example>
///     x = 16;
/// </example>
internal class Assignment : Syntax, IParsableSyntax<Assignment>
{
    public Datum Reference { get; init; }
    public Value Value { get; init; }

    public static Assignment Parse(ref Parser current)
    {
        Parser parser = current;

        if (Grammar.Reference.Parse(ref parser) is not Reference reference) return null;

        if (parser.TryConsume<Assign>() is false) return null;

        if (Value.Parse(ref parser) is not Value value) return null;

        return new Assignment
        {
            Reference = reference,
            Value = value,
            Source = parser.Commit(ref current),
        };
    }
}
