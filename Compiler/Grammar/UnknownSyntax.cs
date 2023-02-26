// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class UnknownSyntax : Syntax, Compiler.IParsable<UnknownSyntax>
{
    public static UnknownSyntax Parse(ref Parser context)
    {
        Parser parser = context;

        if (parser.CurrentToken is Sentinel or TerminalSymbol or SeparatorSymbol or Close) return null;

        do
        {
            parser.Advance();
        }
        while (parser.CurrentToken is not Sentinel and not TerminalSymbol and not SeparatorSymbol and not Close);

        return new UnknownSyntax { Source = parser.Commit(ref context) };
    }
}
