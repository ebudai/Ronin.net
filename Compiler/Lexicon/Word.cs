// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Word : Token
{
    public Word() { }

    public static Word Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty) return null;

        if (char.IsDigit(lexer[0])) return null;

        var length = 0;
        while (length < lexer.Length
            && char.IsWhiteSpace(lexer[length]) is false
            && char.IsSymbol(lexer[length]) is false
            && char.IsPunctuation(lexer[length]) is false) ++length;

        if (length is 0) return null;
        return new Word { Memory = lexer.AdvanceBy(length) };
    }
}
