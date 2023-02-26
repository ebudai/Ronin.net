// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class AtSymbol : Symbol
{
    public const char character = '@';
    public const string symbol = "@";

    public static new AtSymbol Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new AtSymbol { sourcecode = lexer.Commit(symbol.Length) };
    }
}
