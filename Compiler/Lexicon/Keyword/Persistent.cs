// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Keyword;

internal class Persistent : Reserved
{
    internal const string keyword = "persistent";

    public static new Word Lex(ref Lexer lexer)
    {
        if (lexer.DoesNotStartWith(keyword)) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length])) return new Persistent { sourcecode = lexer.Commit(keyword.Length) };
        return null;
    }
}
