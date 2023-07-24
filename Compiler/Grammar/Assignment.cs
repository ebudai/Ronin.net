// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     Sets the current <see cref="Grammar.Value"/> of a <see cref="Grammar.Reference"/>
/// </summary>
/// 
/// <example>
///     x = 16;
/// </example>
internal class Assignment : Statement, IParsableSyntax<Assignment>
{
    public Datum Destination { get; init; }
    public Punctuation Type { get; init; }
    public Value Value { get; init; }

    public new static Assignment Parse(ref Parser current)
    {
        Parser parser = current;

        if (Reference.Parse(ref parser) is not Reference reference) return null;

        if (parser.Token 
            is not Assign
            and not AddAssign
            and not AndAssign
            and not DivideAssign
            and not MultiplyAssign
            and not OrAssign
            and not SubtractAssign) return null;
         
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