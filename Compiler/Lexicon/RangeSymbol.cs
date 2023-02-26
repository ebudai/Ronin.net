// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class RangeSymbol : Punctuation
{
    public const string symbol = "..";

    public static new RangeSymbol Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer.DoesNotStartWith(symbol)) return null;
        return new RangeSymbol { sourcecode = lexer.Commit(symbol.Length) };
    }
}
