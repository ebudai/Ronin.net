// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class CloseSquareBracket : Close
{
    public const char character = ']';
    public const string symbol = "]";

    public static new CloseSquareBracket Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new CloseSquareBracket { sourcecode = lexer.Commit(symbol.Length) };
    }
}
