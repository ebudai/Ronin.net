// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon.Keyword;

namespace Ronin.Grammar;

internal class Modifiers : Syntax, Compiler.IParsableSyntax<Modifiers>
{
    public static Modifiers Parse(ref Parser current)
    {
        Parser parser = current;
        
        while (parser.IsNotFinished)
        {
            if (parser.Token is not Compiled and not Persistent and not Shared and not Optional) break;
            parser.Advance();
        }

        if (current.Token == parser.Token) return null;

        return new Modifiers { Source = parser.Commit(ref current) };
    }
}
