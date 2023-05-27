// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Keyword;

internal class Datatype : Reserved
{
    internal const string keyword = "datatype";
    
    public Datatype()
    {
        sourcecode = keyword.AsMemory();
    }

    public static new Word Lex(ref Lexer lexer)
    {
        if (lexer.DoesNotStartWith(keyword)) return null;
        if (char.IsWhiteSpace(lexer[keyword.Length])) return new Datatype { sourcecode = lexer.Commit(keyword.Length) };
        return null;
    }
}
