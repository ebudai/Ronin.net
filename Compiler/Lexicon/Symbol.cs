// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Symbol : Token
{
    public static Symbol Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty) return null;
        if (char.IsSymbol(lexer[0]) is false && char.IsPunctuation(lexer[0]) is false) return null;
        return new Symbol { Memory = lexer.Commit(1) };
    }
}


