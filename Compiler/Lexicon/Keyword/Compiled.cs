// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Keyword;

internal class Compiled : Reserved
{
    internal const string keyword = "compiled";

    public static new Word Lex(ref Lexer lexer)
    {
        if (lexer.DoesNotStartWith(keyword)) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length])) return new Compiled { sourcecode = lexer.Commit(keyword.Length) };
        return null;
    }
}
