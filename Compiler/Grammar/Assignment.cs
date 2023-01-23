// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Grammar;

/// <summary>
///     An assignment of a <see cref="Grammar.Value"/> to a <see cref="Datum"/> or <see cref="Parameter"/>
/// </summary>
/// 
/// <example>
///     x = 16;
/// </example>
internal class Assignment : Syntax, Compiler.IParsable<Assignment>
{
    public Name Name { get; init; }
    public Argument Argument { get; init; }

    public static Assignment Parse(ref Parser context)
    {
        Parser parser = context;

        if (Name.Parse(ref parser) is not Name name) return null;

        if (parser.Current is not Assign) return null;
        parser.Advance();

        if (Argument.Parse(ref parser) is not Argument argument) return null;

        return new Assignment
        {
            Name = name,
            Argument = argument,
            Source = parser.Commit(ref context),
        };
    }
}
