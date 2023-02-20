// Copyright © 2023 Eric Budai

global using Assign = Ronin.Lexicon.Symbols.Equal;
global using CharacterDelimiter = Ronin.Lexicon.Symbols.Quote;
global using Separator = Ronin.Lexicon.Symbols.Comma;
global using Terminal = Ronin.Lexicon.Symbols.Semicolon;
global using TextDelimiter = Ronin.Lexicon.Symbols.DoubleQuote;

using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Lexicon;

internal class Symbol : Token
{
    public static Symbol Lex(ref Lexer lexer)
        => Ampersand.Lex(ref lexer)
        ?? Returns.Lex(ref lexer) // needs to be above Equals
        ?? Symbols.Range.Lex(ref lexer) // needs to be above Period
        ?? Assign.Lex(ref lexer)
        ?? Asterisk.Lex(ref lexer)
        ?? At.Lex(ref lexer)
        ?? Backslash.Lex(ref lexer)
        ?? Backtick.Lex(ref lexer)
        ?? CharacterDelimiter.Lex(ref lexer)
        ?? Chevron.Lex(ref lexer)
        ?? CloseBrace.Lex(ref lexer)
        ?? CloseParenthesis.Lex(ref lexer)
        ?? CloseSquareBracket.Lex(ref lexer)
        ?? Colon.Lex(ref lexer)
        ?? Separator.Lex(ref lexer)
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
        ?? Terminal.Lex(ref lexer)
        ?? Slash.Lex(ref lexer)
        ?? TextDelimiter.Lex(ref lexer)
        ?? Tilde.Lex(ref lexer) as Symbol;

    internal static bool IsSymbol(ref Lexer lexer, int i = 0)
    {
        var text = lexer[i..];
        if (text.IsEmpty) return false;
        return text.Span.StartsWith(Returns.symbol)
            || text.Span.StartsWith(Symbols.Range.symbol)
            || text.Span[0] is Ampersand.character
            or Assign.character
            or Asterisk.character
            or At.character
            or Backslash.character
            or Backtick.character
            or CharacterDelimiter.character
            or Chevron.character
            or CloseBrace.character
            or CloseParenthesis.character
            or CloseSquareBracket.character
            or Colon.character
            or Separator.character
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
            or Terminal.character
            or Slash.character
            or TextDelimiter.character
            or Tilde.character;
    }
}

internal class Punctuation : Symbol { }