// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class LiteralSyntax : Syntax, Compiler.IParsable<LiteralSyntax>
{
    public static LiteralSyntax Parse(ref Parser context)
    {
        Parser parser = context;

        while (parser.IsNotFinished)
        {            
            if (parser.CurrentToken is not Literal) break;
            parser.Advance();
        }

        if (parser.CurrentToken == context.CurrentToken) return null;

        return new LiteralSyntax { Source = parser.Commit(ref context) };
    }
}