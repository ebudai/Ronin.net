// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Punctuation;

internal class Returns : BreakingSymbol
{
    public const string symbol = "=>";

    public static new Returns Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer.DoesNotStartWith(symbol)) return null;
        return new Returns { sourcecode = lexer.Commit(symbol.Length) };
    }
}
