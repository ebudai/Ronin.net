// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Punctuation;

internal class Range : BreakingSymbol
{
    public const string symbol = "..";

    public static new Range Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer.DoesNotStartWith(symbol)) return null;
        return new Range { sourcecode = lexer.Commit(symbol.Length) };
    }
}
