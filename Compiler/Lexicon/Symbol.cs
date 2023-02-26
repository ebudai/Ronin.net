// Copyright © 2023 Eric Budai

global using AssignSymbol = Ronin.Lexicon.EqualitySymbol;
global using CharacterDelimiterSymbol = Ronin.Lexicon.QuoteSymbol;
global using SeparatorSymbol = Ronin.Lexicon.CommaSymbol;
global using TerminalSymbol = Ronin.Lexicon.SemicolonSymbol;
global using TextDelimiterSymbol = Ronin.Lexicon.DoubleQuoteSymbol;

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Symbol : Token
{
    public static Symbol Lex(ref Lexer lexer)
        => AmpersandSymbol.Lex(ref lexer)
        ?? ReturnsSymbol.Lex(ref lexer) // needs to be above Equals
        ?? RangeSymbol.Lex(ref lexer) // needs to be above Period
        ?? AssignSymbol.Lex(ref lexer)
        ?? AsteriskSymbol.Lex(ref lexer)
        ?? AtSymbol.Lex(ref lexer)
        ?? BackslashSymbol.Lex(ref lexer)
        ?? BacktickSymbol.Lex(ref lexer)
        ?? CharacterDelimiterSymbol.Lex(ref lexer)
        ?? ChevronSymbol.Lex(ref lexer)
        ?? CloseBraceSymbol.Lex(ref lexer)
        ?? CloseParenthesisSymbol.Lex(ref lexer)
        ?? CloseSquareBracketSymbol.Lex(ref lexer)
        ?? ColonSymbol.Lex(ref lexer)
        ?? SeparatorSymbol.Lex(ref lexer)
        ?? DollarSymbol.Lex(ref lexer)
        ?? ExclamationSymbol.Lex(ref lexer)
        ?? GreaterThanSymbol.Lex(ref lexer)
        ?? LessThanSymbol.Lex(ref lexer)
        ?? MinusSymbol.Lex(ref lexer)
        ?? OpenBraceSymbol.Lex(ref lexer)
        ?? OpenParenthesisSymbol.Lex(ref lexer)
        ?? OpenSquareBracketSymbol.Lex(ref lexer)
        ?? PercentSymbol.Lex(ref lexer)
        ?? PeriodSymbol.Lex(ref lexer)
        ?? PipeSymbol.Lex(ref lexer)
        ?? PlusSymbol.Lex(ref lexer)
        ?? PoundSymbol.Lex(ref lexer)
        ?? QuestionSymbol.Lex(ref lexer)
        ?? TerminalSymbol.Lex(ref lexer)
        ?? SlashSymbol.Lex(ref lexer)
        ?? TextDelimiterSymbol.Lex(ref lexer)
        ?? TildeSymbol.Lex(ref lexer) as Symbol;

    internal static bool IsSymbol(ref Lexer lexer, int i = 0)
    {
        var text = lexer[i..];
        if (text.IsEmpty) return false;
        return text.Span.StartsWith(ReturnsSymbol.symbol)
            || text.Span.StartsWith(RangeSymbol.symbol)
            || text.Span[0] is AmpersandSymbol.character
            or AssignSymbol.character
            or AsteriskSymbol.character
            or AtSymbol.character
            or BackslashSymbol.character
            or BacktickSymbol.character
            or CharacterDelimiterSymbol.character
            or ChevronSymbol.character
            or CloseBraceSymbol.character
            or CloseParenthesisSymbol.character
            or CloseSquareBracketSymbol.character
            or ColonSymbol.character
            or SeparatorSymbol.character
            or DollarSymbol.character
            or ExclamationSymbol.character
            or GreaterThanSymbol.character
            or LessThanSymbol.character
            or MinusSymbol.character
            or OpenBraceSymbol.character
            or OpenParenthesisSymbol.character
            or OpenSquareBracketSymbol.character
            or PercentSymbol.character
            or PeriodSymbol.character
            or PipeSymbol.character
            or PlusSymbol.character
            or PoundSymbol.character
            or QuestionSymbol.character
            or TerminalSymbol.character
            or SlashSymbol.character
            or TextDelimiterSymbol.character
            or TildeSymbol.character;
    }
}

internal class Punctuation : Symbol { }