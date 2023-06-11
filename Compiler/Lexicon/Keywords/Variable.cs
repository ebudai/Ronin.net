// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Keywords;

internal class Variable : Keyword
{
    internal const string keyword = "var";

    public static new Keyword Lex(scoped ref Lexer lexer)
    {
        if (lexer.DoesNotStartWith(keyword)) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length]) is false) return null;
        return new Variable { Memory = lexer.Commit(keyword.Length) };
    }
}
