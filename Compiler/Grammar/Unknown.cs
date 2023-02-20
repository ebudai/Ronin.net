// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon.Symbols;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Unknown : Syntax, Compiler.IParsable<Unknown>
{
    public static Unknown Parse(ref Parser context)
    {
        Parser parser = context;

        if (parser.CurrentToken is Sentinel or Terminal or Separator or Close) return null;

        do
        {
            parser.Advance();
        }
        while (parser.CurrentToken is not Sentinel and not Terminal and not Separator and not Close);

        return new Unknown { Source = parser.Commit(ref context) };
    }
}
