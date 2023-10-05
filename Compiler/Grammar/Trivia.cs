// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System;

namespace Ronin.Grammar;

/// <summary>
///     Represents a <see cref="Whitespace"/> or a <see cref="Comment"/>
/// </summary>
internal class Trivia : IGrammar<Trivia>
{
    public ReadOnlyMemory<Token> Tokens { get; init; }

    public static Trivia Parse(ref Parser current)
    {
        Parser parser = current;

        if (parser.TryAdvanceMany<Trivium>() is false) return null;

        return new Trivia { Tokens = current.AdvanceTo(parser) };
    }
}
