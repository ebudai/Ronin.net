// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class PercentSymbol : Symbol
{
    public const char character = '%';
    public const string symbol = "%";

    public static new PercentSymbol Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new PercentSymbol { sourcecode = lexer.Commit(symbol.Length) };
    }
}
