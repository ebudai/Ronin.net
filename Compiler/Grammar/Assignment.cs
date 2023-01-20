// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Grammar;

/// <summary>
///     An assignment of one value to a datum or parameter
/// </summary>
internal class Assignment : Syntax, Compiler.IParsable<Assignment>
{
    public Name Name { get; init; }
    public Value Value { get; init; }

    public static Assignment Parse(ref Parser context)
    {
        Parser parser = context;

        if (Name.Parse(ref parser) is not Name name) return null;

        if (parser.Current is not Assign) return null;
        parser.Advance();

        if (Value.Parse(ref parser) is not Value value) return null;

        return new Assignment
        {
            Name = name,
            Value = value,
            Source = parser.Commit(ref context),
        };
    }
}
