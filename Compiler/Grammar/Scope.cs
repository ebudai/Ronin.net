using Ronin.Compiler;
using Ronin.Grammar.Compound;
using Ronin.Lexicon.Keywords;

namespace Ronin.Grammar;

internal class Scope : Statement, IParsableSyntax<Scope>
{
    public bool IsCompiled { get; init; }
    public Definition Definition { get; init; }

    public static new Scope Parse(scoped ref Parser current)
    {
        Parser parser = current;

        var isCompiled = parser.TryAdvance<Compiled>();

        if (Definition.Parse(ref parser) is not Definition definition) return null;

        return new Scope
        {
            IsCompiled = isCompiled,
            Definition = definition,
            Source = parser.Commit(ref current)
        };
    }
}