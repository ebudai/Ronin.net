// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using System;

namespace Ronin.Lexicon;

internal class Character : Literal
{
    public static new Token Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not CharacterDelimiter.symbol) return null;

        var length = lexer[1..].IndexOf(CharacterDelimiter.symbol); // find the closing delimiter one

        if (length is not 1 and not 6) return null;

        if (length is 6)
        {
            for (var i = 3; i != length; ++i)
            {
                if (IsValid(lexer[i]) is false) return null;
            }
        }

        return new Character { Memory = lexer.Commit(length + 2) };
    }

    private static bool IsValid(char character)
        => char.IsDigit(character)
        || character
            is 'A' or 'a'
            or 'B' or 'b'
            or 'C' or 'c'
            or 'D' or 'd'
            or 'E' or 'e'
            or 'F' or 'f';
}
