// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Punctuation;

internal class CloseBrace : Close
{
    public const char character = '}';
    public const string symbol = "}";

    public CloseBrace()
    {
        sourcecode = symbol.AsMemory();
    }

    public static new CloseBrace Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new CloseBrace { sourcecode = lexer.Commit(symbol.Length) };
    }
}
