// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Keywords;

internal class ForEach : Keyword
{
    internal const string keyword = "for each";

    public static new Keyword Lex(ref Lexer lexer)
    {
        if (lexer.DoesNotStartWith(keyword)) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length]) is false) return null;
        return new ForEach { sourcecode = lexer.Commit(keyword.Length) };
    }
}
