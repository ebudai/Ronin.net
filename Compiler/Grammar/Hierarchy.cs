// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Reserved;

namespace Ronin.Grammar;

/// <summary>
///     Names a <see cref="Scope"/> via 'part of', or exposes one to the current <see cref="Scope"/> via 'import'
/// </summary>
/// 
/// <example>
///     part of best package for weather lookups
///     import best package for weather lookups
///     import git://github.com/ebudai/Ronin
/// </example>
/// 
internal class Hierarchy : Syntax, IParsable
{
    public Keyword Direction { get; init; }
    public Name Name { get; init; }

    public static Syntax Parse(ref Parser context)
    {
        var direction = context.Current is PartOf or Import ? context.Current as Keyword : null;
        if (direction is null) return null;

        Parser parser = context;
        parser.Advance();

        var name = Name.Parse(ref parser);
        if (name is null) return null;

        return new Hierarchy 
        {
            Direction = direction,
            Name = name as Name,             
            Source = parser.Commit(ref context) 
        };
    }
}