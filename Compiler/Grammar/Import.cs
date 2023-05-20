using Ronin.Compiler;

namespace Ronin.Grammar;

/// <summary>
///     Names a <see cref="Scope"/> via 'part of', or exposes one to the current <see cref="Scope"/> via 'import'
/// </summary>
/// 
/// <example>
///     part of best package for weather lookups
///     
///     import best package for weather lookups
///     import git://github.com/ebudai/Ronin as ronin
/// </example>
internal class Import : Statement, IParsableSyntax<Import>
{
    public Name Name { get; init; }

    public new static Import Parse(ref Parser current)
    {
        if (current.Token is not Lexicon.Keyword.Import) return null;

        Parser parser = current;
        parser.Advance();

        if (Name.Parse(ref parser) is not Name name) return null;

        return new Import
        {
            Name = name,
            Source = parser.Commit(ref current)
        };
    }
}
