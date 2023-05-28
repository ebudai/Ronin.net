// Copyright © 2023 Eric Budai

global using Assign = Ronin.Lexicon.Punctuation.Equality;
global using CharacterDelimiter = Ronin.Lexicon.Punctuation.Quote;
global using Separator = Ronin.Lexicon.Punctuation.Comma;
global using Terminal = Ronin.Lexicon.Punctuation.Semicolon;
global using TextDelimiter = Ronin.Lexicon.Punctuation.DoubleQuote;

using Ronin.Lexicon.Punctuation;
using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Symbol : Token
{
    public static Symbol Lex(ref Lexer lexer)
        => Ampersand.Lex(ref lexer)
        ?? Returns.Lex(ref lexer) // needs to be above Equals
        ?? Punctuation.Range.Lex(ref lexer) // needs to be above Period
        ?? Equality.Lex(ref lexer)
        ?? Asterisk.Lex(ref lexer)
        ?? At.Lex(ref lexer)
        ?? Backslash.Lex(ref lexer)
        ?? Backtick.Lex(ref lexer)
        ?? Quote.Lex(ref lexer)
        ?? Chevron.Lex(ref lexer)
        ?? CloseBrace.Lex(ref lexer)
        ?? CloseParenthesis.Lex(ref lexer)
        ?? CloseSquareBracket.Lex(ref lexer)
        ?? Colon.Lex(ref lexer)
        ?? Comma.Lex(ref lexer)
        ?? Dollar.Lex(ref lexer)
        ?? Exclamation.Lex(ref lexer)
        ?? GreaterThan.Lex(ref lexer)
        ?? LessThan.Lex(ref lexer)
        ?? Minus.Lex(ref lexer)
        ?? OpenBrace.Lex(ref lexer)
        ?? OpenParenthesis.Lex(ref lexer)
        ?? OpenSquareBracket.Lex(ref lexer)
        ?? Percent.Lex(ref lexer)
        ?? Period.Lex(ref lexer)
        ?? Pipe.Lex(ref lexer)
        ?? Plus.Lex(ref lexer)
        ?? Pound.Lex(ref lexer)
        ?? Question.Lex(ref lexer)
        ?? Semicolon.Lex(ref lexer)
        ?? Slash.Lex(ref lexer)
        ?? DoubleQuote.Lex(ref lexer)
        ?? Tilde.Lex(ref lexer) as Symbol;

    internal static bool IsSymbol(ref Lexer lexer, int i = 0)
    {
        var text = lexer[i..];
        if (text.IsEmpty) return false;
        return text.Span.StartsWith(Returns.symbol)
            || text.Span.StartsWith(Punctuation.Range.symbol)
            || text.Span[0] is Ampersand.character
            or Equality.character
            or Asterisk.character
            or At.character
            or Backslash.character
            or Backtick.character
            or Quote.character
            or Chevron.character
            or CloseBrace.character
            or CloseParenthesis.character
            or CloseSquareBracket.character
            or Colon.character
            or Comma.character
            or Dollar.character
            or Exclamation.character
            or GreaterThan.character
            or LessThan.character
            or Minus.character
            or OpenBrace.character
            or OpenParenthesis.character
            or OpenSquareBracket.character
            or Percent.character
            or Period.character
            or Pipe.character
            or Plus.character
            or Pound.character
            or Question.character
            or Semicolon.character
            or Slash.character
            or DoubleQuote.character
            or Tilde.character;
    }

    public static bool IsNotSymbol(ref Lexer lexer, int i = 0) => IsSymbol(ref lexer, i) is false;
}

internal class BreakingSymbol : Symbol { }