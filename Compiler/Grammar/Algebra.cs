// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;

namespace Ronin.Grammar;

/// <summary>
///     Union of <see cref="Scalar"/>, <see cref="Arguments"/>, and <see cref="Name"/>
/// </summary>
/// 
/// <remarks>
///     This is used to produce unions and subclasses for datatypes
/// </remarks>
/// 
/// <example>
///     datatype Squirrel = Mammal and 
///     {
///         var tail fluffiness => number;
///         var home => Tree;
///     }
/// </example>
internal class Algebra : Syntax, Compiler.IParsable<Algebra>
{
    public Syntax Syntax { get; init; }

    public static Algebra Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = Scalar.Parse(ref parser)
            ?? Arguments.Parse(ref parser)
            ?? Name.Parse(ref parser) as Syntax;

        if (syntax is null) return null;

        return new Algebra { Syntax = syntax, Source = parser.Commit(ref context) };
    }
}