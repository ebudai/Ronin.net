// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon.Keywords;

namespace Ronin.Grammar;

internal class Modifiers : Syntax, Compiler.IParsable<Modifiers>
{
    public static Modifiers Parse(ref Parser context)
    {
        Parser parser = context;
        
        while (parser.IsNotFinished)
        {
            if (parser.CurrentToken is not Compiled and not Persistent and not Shared and not Optional) break;
            parser.Advance();
        }

        if (context.CurrentToken == parser.CurrentToken) return null;

        return new Modifiers { Source = parser.Commit(ref context) };
    }
}
