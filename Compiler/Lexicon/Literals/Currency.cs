// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using System.Globalization;

namespace Ronin.Lexicon.Literals;

internal class Currency : Literal
{
    public static new Token Lex(ref Lexer lexer)
    {
        if (lexer.Length is < 2
            || CharUnicodeInfo.GetUnicodeCategory(lexer[0]) is not UnicodeCategory.CurrencySymbol
            || char.IsDigit(lexer[1]) is false) return null;

        int length = 2;
        bool hasPeriod = false;
        for (int max = lexer.Length; length != max; ++length)
        {
            var character = lexer[length];

            if (char.IsDigit(character) is false && lexer[length] is not '_' and not '.') break;

            if (character is '.')
            {
                if (hasPeriod) break;
                hasPeriod = true;
            }
        }

        if (lexer[length - 1] is '.') --length;

        return new Currency { Memory = lexer.Commit(length) };
    }
}
