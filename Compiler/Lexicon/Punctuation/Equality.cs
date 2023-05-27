// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon.Punctuation;

internal class Equality : BreakingSymbol
{
    public const char character = '=';
    public const string symbol = "=";

    public Equality() 
    { 
        sourcecode = symbol.AsMemory(); 
    }

    public static new Equality Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not character) return null;
        return new Equality { sourcecode = lexer.Commit(symbol.Length) };
    }
}
