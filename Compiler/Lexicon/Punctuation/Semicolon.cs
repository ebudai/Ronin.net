// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Punctuation;

internal class Semicolon : BreakingSymbol
{
    public const char character = ';';
    public const string symbol = ";";

    public Semicolon()
    {
        sourcecode = symbol.AsMemory();
    }

    public static new Semicolon Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new Semicolon { sourcecode = lexer.Commit(symbol.Length) };
    }
}
