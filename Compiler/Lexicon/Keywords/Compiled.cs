// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Keywords;

internal class Compiled : Keyword
{
    internal const string keyword = "compiled";

    public static new Keyword Lex(scoped ref Lexer lexer)
    {
        if (lexer.DoesNotStartWith(keyword)) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length]) is false) return null;
        return new Compiled { Memory = lexer.Commit(keyword.Length) };
    }
}
