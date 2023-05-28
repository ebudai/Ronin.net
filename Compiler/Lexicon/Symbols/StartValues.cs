// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class StartValues : Punctuation
{
    internal const char symbol = '(';

    public static new StartValues Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not symbol) return null;
        return new() { sourcecode = lexer.Commit(1) };
    }
}
