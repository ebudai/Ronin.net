// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Interval : Symbol
{
    internal const string symbol = "..";

    public static new Interval Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || symbol.StartsWith(lexer[0]) is false) return null;
        return new() { Memory = lexer.Commit(symbol.Length) };
    }
}
