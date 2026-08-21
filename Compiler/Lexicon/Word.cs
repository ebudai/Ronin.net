// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Word : Token
{
    public Word() { }

    public static Word Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty) return null;

        // A word may not START where a number does — but "where a number starts" is the ASCII
        // «0-9» the numeral alphabet rules (NUMERALALPHABET), not «char.IsDigit». A Unicode
        // decimal digit «Numeric» now refuses is NOT a number, so it may begin a name here
        // rather than being a run no token consumes — which would spin «Lexer.Lex» forever.
        if (Numeric.Digit(lexer[0])) return null;

        var length = 0;
        while (length < lexer.Length
            && char.IsWhiteSpace(lexer[length]) is false
            && char.IsSymbol(lexer[length]) is false
            && char.IsPunctuation(lexer[length]) is false) ++length;

        if (length is 0) return null;
        return new Word { Memory = lexer.AdvanceBy(length) };
    }
}
