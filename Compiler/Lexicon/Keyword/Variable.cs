// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Keyword;

internal class Variable : Reserved
{
    internal const string keyword = "var";

    public static new Word Lex(ref Lexer lexer)
    {
        if (lexer.DoesNotStartWith(keyword)) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length])) return new Variable { sourcecode = lexer.Commit(keyword.Length) };
        return null;
    }
}
