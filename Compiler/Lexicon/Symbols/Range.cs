// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Range : Punctuation
{
    internal const string symbol = "..";

    public static new Range Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || symbol.StartsWith(lexer[0]) is false) return null;
        return new() { Memory = lexer.Commit(symbol.Length) };
    }
}
