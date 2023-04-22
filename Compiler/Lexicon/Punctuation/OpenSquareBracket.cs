// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Punctuation;

internal class OpenSquareBracket : Open
{
    public const char character = '[';
    public const string symbol = "[";

    public static new OpenSquareBracket Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new OpenSquareBracket { sourcecode = lexer.Commit(symbol.Length) };
    }
}
