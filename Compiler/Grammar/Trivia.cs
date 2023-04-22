// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Trivia : Syntax, IParsableSyntax<Trivia>
{
    public static Trivia Parse(ref Parser context)
    {
        Parser parser = context;
        if (parser.FailsToConsume<Trivium>()) return null;
        return new Trivia { Source = parser.Commit(ref context) };
    }
}
