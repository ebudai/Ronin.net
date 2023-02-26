// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class AmpersandSymbol : Symbol
{
    public const char character = '&';
    public const string symbol = "&";

    public static new AmpersandSymbol Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new AmpersandSymbol { sourcecode = lexer.Commit(symbol.Length) };
    }
}
