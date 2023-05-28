// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Returns : Punctuation
{
    internal const string symbol = "=>";

    public static new Returns Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer.StartsWith(symbol) is false) return null;
        return new Returns { sourcecode = lexer.Commit(symbol.Length) };
    }
}
