// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class OpenParenthesisSymbol : Open
{
    public const char character = '(';
    public const string symbol = "(";

    public static new OpenParenthesisSymbol Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new OpenParenthesisSymbol { sourcecode = lexer.Commit(symbol.Length) };
    }
}
