// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Keyword;

internal class Reactive : Reserved
{
    internal const string keyword = "reactive";

    
    public static new Word Lex(ref Lexer lexer)
    {
        if (lexer.DoesNotStartWith(keyword)) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length])) return new Reactive { sourcecode = lexer.Commit(keyword.Length) };
        return null;
    }
}
