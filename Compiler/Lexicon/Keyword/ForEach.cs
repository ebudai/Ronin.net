// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Keyword;

internal class ForEach : Reserved
{
    internal const string keyword = "for each";

    public static new Word Lex(ref Lexer lexer)
    {
        if (lexer.DoesNotStartWith(keyword)) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length])) return new ForEach { sourcecode = lexer.Commit(keyword.Length) };
        return null;
    }
}
