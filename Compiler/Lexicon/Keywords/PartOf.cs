// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Keywords;

internal class PartOf : Keyword
{
    internal const string keyword = "part of";

    public static new Keyword Lex(ref Lexer lexer)
    {
        if (lexer.DoesNotStartWith(keyword)) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length]) is false) return null;
        return new PartOf { sourcecode = lexer.Commit(keyword.Length) };
    }
}
