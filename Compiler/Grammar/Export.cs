// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

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
    public Keyword Keyword { get; init; }
    public Name Name { get; init; }

    public new static Export Parse(ref Parser current)
    {
        Parser parser = current;

        if (parser.Token is not PartOf keyword) return null;
        parser.Advance();

        if (Name.Parse(ref parser) is not Name name) return null;

        return new Export 
        {
            Keyword = keyword,
            Name = name,
            Source = parser.Commit(ref current) 
        };
    }
}