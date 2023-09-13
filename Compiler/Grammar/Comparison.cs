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
internal class Comparison : Value, IParsableSyntax<Comparison>
{
    public Value Left { get; set; }
    public Assign Operation { get; init; }
    public Value Right { get; set; }

    public new static Comparison Parse(ref Parser current)
    {
        Parser parser = current;

        if (Reference.Parse(ref parser) is not Reference reference) return null;

        if (parser.Token is not Assign operation) return null;
        parser.Advance();

        if (Value.Parse(ref parser) is not Value value) return null;

        return new Comparison
        {
            Left = new Datum.Unresolved { Reference = reference },
            Operation = operation,
            Right = value,
            Source = parser.Commit(ref current),
        };
    }
}

internal class Condition : UnionSyntax<Condition, Value, Comparison> 
{
    public static implicit operator Condition(Value value) => new() { value = value, Source = value.Source };
    public static implicit operator Condition(Comparison comparison) => new() { value = comparison, Source = comparison.Source };
}