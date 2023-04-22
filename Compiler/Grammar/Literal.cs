// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Literal : Syntax, IParsableSyntax<Literal>
{
    public static Literal Parse(ref Parser context)
    {
        Parser parser = context;

        while (parser.IsNotFinished)
        {            
            if (parser.CurrentToken is not Lexicon.Literal) break;
            parser.Advance();
        }

        if (parser.CurrentToken == context.CurrentToken) return null;

        return new Literal { Source = parser.Commit(ref context) };
    }
}