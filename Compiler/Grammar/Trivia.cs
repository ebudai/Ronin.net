// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Trivia : Syntax, Compiler.IParsable<Trivia>
{
    public static Trivia Parse(ref Parser context)
    {
        Parser parser = context;
        while (parser.CurrentToken is Trivium) parser.Advance();
        if (parser.CurrentToken is Terminal) parser.Advance();
        return parser.CurrentToken == context.CurrentToken ? null : new Trivia { Source = parser.Commit(ref context) };
    }
}
