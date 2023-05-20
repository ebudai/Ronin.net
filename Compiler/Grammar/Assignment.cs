// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Grammar;

/// <summary>
///     Sets the current <see cref="Grammar.Anonymous"/> of a <see cref="Datum"/>
/// </summary>
/// 
/// <example>
///     x = 16;
/// </example>
internal class Assignment : Statement, IParsableSyntax<Assignment>
{
    public Reference Reference { get; init; }
    public Anonymous Value { get; init; }

    public new static Assignment Parse(ref Parser current)
    {
        Parser parser = current;

        if (Reference.Parse(ref parser) is not Reference reference) return null;

        if (parser.TryAdvance<Assign>() is false) return null;

        if (Anonymous.Parse(ref parser) is not Anonymous value) return null;

        return new Assignment
        {
            Reference = reference,
            Value = value,
            Source = parser.Commit(ref current),
        };
    }
}