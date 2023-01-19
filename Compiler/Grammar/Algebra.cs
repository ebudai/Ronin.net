// Copyright © 2023 Eric Budai

using Ronin.Grammar.Aggregates;
using Ronin.Compiler;


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
internal class Algebra : Syntax, IParsable
{
    public Syntax Syntax { get; init; }

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = Scalar.Parse(ref parser)
            ?? Arguments.Parse(ref parser)
            ?? Name.Parse(ref parser);

        if (syntax is Error or null) return syntax;

        return new Algebra { Syntax = syntax, Source = parser.Commit(ref context) };
    }
}