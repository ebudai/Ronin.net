// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Grammar;

/// <summary>
///     An assignment of a <see cref="Grammar.Temporary"/> to a <see cref="Datum"/> or <see cref="Parameter"/>
/// </summary>
/// 
/// <example>
///     x = 16;
/// </example>
internal class Assignment : Syntax, Compiler.IParsable<Assignment>
{
    public Reference Reference { get; init; }
    public Value Value { get; init; }

    public static Assignment Parse(ref Parser context)
    {
        Parser parser = context;

        if (Reference.Parse(ref parser) is not Reference reference) return null;

        if (parser.CurrentToken is not Assign) return null;
        parser.Advance();

        if (Value.Parse(ref parser) is not Value value) return null;

        return new Assignment
        {
            Reference = reference,
            Value = value,
            Source = parser.Commit(ref context),
        };
    }
}
