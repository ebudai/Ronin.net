// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Literal : Syntax, IParsableSyntax<Literal>
{
    public static Literal Parse(ref Parser current)
    {
        Parser parser = current;

        while (parser.IsNotFinished)
        {            
            if (parser.Token is not Lexicon.Literal) break;
            parser.Advance();
        }

        if (parser.Token == current.Token) return null;

        return new Literal { Source = parser.Commit(ref current) };
    }
}