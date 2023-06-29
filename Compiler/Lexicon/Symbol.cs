// Copyright © 2023 Eric Budai

using Ronin.Lexicon.Symbols;
using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Symbol : Token
{
    public static Symbol Lex(ref Lexer lexer)
    {
        if (Punctuation.Lex(ref lexer) is Punctuation punctuation) return punctuation;
        if (Interval.Lex(ref lexer) is Interval interval) return interval;
        if (CharacterDelimiter.Lex(ref lexer) is CharacterDelimiter character) return character;

        if (lexer.IsEmpty) return null;

        if (char.IsSymbol(lexer[0]) is false && char.IsPunctuation(lexer[0]) is false) return null;

        return new Symbol { Memory = lexer.Commit(1) };
    }
}

internal class Punctuation : Symbol 
{
    public static new Punctuation Lex(ref Lexer lexer)
        => Returns.Lex(ref lexer)
        ?? Assign.Lex(ref lexer)
        ?? AddAssign.Lex(ref lexer)
        ?? AndAssign.Lex(ref lexer)
        ?? DivideAssign.Lex(ref lexer)
        ?? MultiplyAssign.Lex(ref lexer)
        ?? OrAssign.Lex(ref lexer)
        ?? SubtractAssign.Lex(ref lexer)
        ?? EndOrdinal.Lex(ref lexer)
        ?? EndScope.Lex(ref lexer)
        ?? EndValues.Lex(ref lexer)
        ?? Separator.Lex(ref lexer)
        ?? StartOrdinal.Lex(ref lexer)
        ?? StartScope.Lex(ref lexer)
        ?? StartValues.Lex(ref lexer)
        ?? Terminal.Lex(ref lexer)
        ?? TextDelimiter.Lex(ref lexer) as Punctuation;
}