// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class EndOrdinal : Punctuation
{
    internal const char symbol = ']';

    public static new EndOrdinal Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not symbol) return null;
        return new() { Memory = lexer.Commit(1) };
    }
}
