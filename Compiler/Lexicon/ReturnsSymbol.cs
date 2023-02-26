// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class ReturnsSymbol : Punctuation
{
    public const string symbol = "=>";

    public static new ReturnsSymbol Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer.DoesNotStartWith(symbol)) return null;
        return new ReturnsSymbol { sourcecode = lexer.Commit(symbol.Length) };
    }
}
