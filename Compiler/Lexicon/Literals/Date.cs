// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Literals;

internal class Date : Literal
{
    public static new Token Lex(ref Lexer lexer)
    {
        if (lexer.Length is < Length) return null;

        if (char.IsDigit(lexer[0]) is not true) return null;
        if (char.IsDigit(lexer[1]) is not true) return null;
        if (char.IsDigit(lexer[2]) is not true) return null;
        if (char.IsDigit(lexer[3]) is not true) return null;
        if (lexer[4] is not '-') return null;
        if (char.IsDigit(lexer[5]) is not true) return null;
        if (char.IsDigit(lexer[6]) is not true) return null;
        if (lexer[7] is not '-') return null;
        if (char.IsDigit(lexer[8]) is not true) return null;
        if (char.IsDigit(lexer[9]) is not true) return null;

        return new Date { sourcecode = lexer.Commit(Length) };
    }

    private const int Length = 10;
}
