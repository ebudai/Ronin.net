// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     Represents a <see cref="Whitespace"/> or a <see cref="Comment"/>
/// </summary>
internal class Trivia : Syntax, IParsableSyntax<Trivia>
{
    public static Trivia Parse(ref Parser current)
    {
        Parser parser = current;
        if (parser.TryParse<Trivium>() is null) return null;
        return new Trivia { Source = parser.Commit(ref current) };
    }
}
