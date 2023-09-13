// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class If : Keyword
{
    internal const string keyword = "if";

    public static new Keyword Lex(ref Lexer lexer)
    {
        if (lexer.StartsWith(keyword) is false) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length]) is false) return null;
        return new If { Memory = lexer.Commit(keyword.Length) };
    }
}
