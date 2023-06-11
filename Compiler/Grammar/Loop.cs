// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Compound;
using Ronin.Lexicon.Keywords;

namespace Ronin.Grammar;

/// <summary>
///     Represents a loop construct in which the same <see cref="Compound.Definition"/> or <see cref="Statement"/>
///     is executed multiple times
/// </summary>
/// 
/// <example>
///     for each car in cars 
///     { 
///         car name = random; 
///     }
/// </example>
internal class Loop : Syntax, IParsableSyntax<Loop>
{
    public DatumDeclaration Header { get; init; }
    public Reference List { get; init; }
    public Definition Definition { get; init; }

    public static Loop Parse(scoped ref Parser current)
    {
        Parser parser = current;

        if (parser.TryAdvance<ForEach>() is false) return null;

        if (DatumDeclaration.Parse(ref parser) is not DatumDeclaration header) return null;

        var list = header.Datatype is null ? null : Reference.Parse(ref parser);

        if (Definition.Parse(ref parser) is not Definition definition) return null;

        return new Loop
        {
            Header = header,
            List = list,
            Definition = definition,
            Source = parser.Commit(ref current)
        };
    }
}
