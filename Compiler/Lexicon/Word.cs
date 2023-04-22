// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Word : Token
{
    public static Word Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty) return null;

        if (char.IsNumber(lexer[0])) return null;

        var length = 0;
        while (length < lexer.Length
            && char.IsWhiteSpace(lexer[length]) is false
            && Symbol.IsNotSymbol(ref lexer, length)) ++length;

        if (length is 0) return null;
        return new Word { sourcecode = lexer.Commit(length) };
    }
}