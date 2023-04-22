// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Punctuation;

internal class Tilde : Symbol
{
    public const char character = '~';
    public const string symbol = "~";

    public static new Tilde Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new Tilde { sourcecode = lexer.Commit(symbol.Length) };
    }
}
