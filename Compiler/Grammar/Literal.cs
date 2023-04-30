// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Literal : Anonymous, IParsableSyntax<Literal>
{
    public new static Literal Parse(ref Parser current)
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