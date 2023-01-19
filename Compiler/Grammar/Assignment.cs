// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Grammar;

/// <summary>
/// 
/// </summary>
internal class Assignment : Syntax, IParsable
{
    public Name Name { get; init; }
    public Value Value { get; init; }

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;

        if (Name.Parse(ref parser) is not Name name) return null;

        if (parser.Current is not Assign) return null;
        parser.Advance();

        var value = Value.Parse(ref parser);
        if (value is Error or null) return value;

        return new Assignment
        {
            Name = name,
            Value = value as Value,
            Source = parser.Commit(ref context),
        };
    }
}
