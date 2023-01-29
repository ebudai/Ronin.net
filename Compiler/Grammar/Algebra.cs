// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;

namespace Ronin.Grammar;

/// <summary>
///     This is used to produce unions and child classes for datatypes
/// </summary>
/// 
/// <remarks>
///     Union of <see cref="Scalar"/>, <see cref="Arguments"/>, and <see cref="Name"/>
/// </remarks>
/// 
/// <example>
///     datatype Squirrel = Mammal and 
///                         ↑↑↑↑↑↑↑↑↑↑
///     {
///         var tail fluffiness => number;
///         var home => Tree;
///     }
/// </example>
internal class Algebra : Syntax, Compiler.IParsable<Algebra>
{
    public static Algebra Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = Scalar.Parse(ref parser)
            ?? Arguments.Parse(ref parser)
            ?? Name.Parse(ref parser) as Syntax;

        if (syntax is null) return null;

        return new Algebra { value = syntax, Source = parser.Commit(ref context) };
    }

    public static implicit operator Scalar(Algebra algebra) => algebra.value as Scalar;
    public static implicit operator Arguments(Algebra algebra) => algebra.value as Arguments;
    public static implicit operator Name(Algebra algebra) => algebra.value as Name;

    private Syntax value;
}