// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Unknown : Syntax, IParsableSyntax<Unknown>
{
    public static Unknown Parse(ref Parser current)
    {
        Parser parser = current;

        if (parser.Token is Sentinel or Terminal or Separator or Close) return null;

        do
        {
            parser.Advance();
        }
        while (parser.Token is not Sentinel and not Terminal and not Separator and not Close);

        return new Unknown { Source = parser.Commit(ref current) };
    }
}
