// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class GreaterThan : Symbol
{
    public const char character = '>';
    public const string symbol = ">";

    public static new GreaterThan Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new GreaterThan { sourcecode = lexer.Commit(symbol.Length) };
    }
}
