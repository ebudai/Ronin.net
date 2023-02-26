// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class DoubleQuoteSymbol : Punctuation
{
    public const char character = '"';
    public const string symbol = "\"";

    public static new DoubleQuoteSymbol Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new DoubleQuoteSymbol { sourcecode = lexer.Commit(symbol.Length) };
    }
}
