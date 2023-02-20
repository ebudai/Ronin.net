// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Period : Symbol
{
    public const char character = '.';
    public const string symbol = ".";

    public static new Period Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new Period { sourcecode = lexer.Commit(symbol.Length) };
    }
}
