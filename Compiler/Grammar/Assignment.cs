// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

/// <summary>
///     Sets the current <see cref="Grammar.Value"/> of a <see cref="DatumDeclaration"/>
/// </summary>
/// 
/// <example>
///     x = 16;
/// </example>
internal class Assignment : Statement, IParsableSyntax<Assignment>
{
    public Reference Reference { get; init; }
    public Value Value { get; init; }

    public new static Assignment Parse(scoped ref Parser current)
    {
        Parser parser = current;

        if (Reference.Parse(ref parser) is not Reference reference) return null;

        if (parser.TryAdvance<Assign>() is false) return null;

        if (Value.Parse(ref parser) is not Value value) return null;

        return new Assignment
        {
            Reference = reference,
            Value = value,
            Source = parser.Commit(ref current),
        };
    }
}