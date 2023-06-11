// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon.Keywords;

namespace Ronin.Grammar;

/// <summary>
///     Exposes one to the current <see cref="Scope"/> via 'import'
/// </summary>
/// 
/// <example>
///     part of best package for weather lookups
///     
///     import best package for weather lookups
///     import git://github.com/ebudai/Ronin as ronin
/// </example>
internal class Export : Statement, IParsableSyntax<Export>
{
    public Words Name { get; init; }

    public new static Export Parse(scoped ref Parser current)
    {
        if (current.Token is not PartOf) return null;

        Parser parser = current;
        parser.Advance();

        if (Words.Parse(ref parser) is not Words name) return null;

        return new Export 
        {
            Name = name,
            Source = parser.Commit(ref current) 
        };
    }
}