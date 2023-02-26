// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class QuestionSymbol : Symbol
{
    public const char character = '?';
    public const string symbol = "?";

    public static new QuestionSymbol Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new QuestionSymbol { sourcecode = lexer.Commit(symbol.Length) };
    }
}
