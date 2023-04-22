// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Punctuation;

internal class Exclamation : Symbol
{
    public const char character = '!';
    public const string symbol = "!";

    public static new Exclamation Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new Exclamation { sourcecode = lexer.Commit(symbol.Length) };
    }
}
