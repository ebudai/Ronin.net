//global using Terminal = Ronin.Lexicon.Symbols.Semicolon;
//global using Separator = Ronin.Lexicon.Symbols.Comma;

using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Lexicon;

internal class Symbol : Token
{
    internal Symbol(Lexer lexer, int length) : base(lexer, length) { }

    internal static bool IsSymbol(Lexer lexer, int i = 0)
    {
        var text = lexer[i..].Span;
        if (text.IsEmpty) return false;
        return text.StartsWith(Returns.symbol)
            || text[0] is Ampersand.character
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
            or TextDelimiter.character
            or Tilde.character;
    }

    internal static Symbol Lex(Lexer lexer)
        => Ampersand.Lex(lexer)
        ?? Returns.Lex(lexer) // needs to be above Assign
        ?? Assign.Lex(lexer)
        ?? Asterisk.Lex(lexer)
        ?? At.Lex(lexer)
        ?? Backslash.Lex(lexer)
        ?? Backtick.Lex(lexer)
        ?? CharacterDelimiter.Lex(lexer)
        ?? Chevron.Lex(lexer)
        ?? CloseBrace.Lex(lexer)
        ?? CloseParenthesis.Lex(lexer)
        ?? CloseSquareBracket.Lex(lexer)
        ?? Colon.Lex(lexer)
        ?? Comma.Lex(lexer)
        ?? Dollar.Lex(lexer)
        ?? Exclamation.Lex(lexer)
        ?? GreaterThan.Lex(lexer)
        ?? LessThan.Lex(lexer)
        ?? Minus.Lex(lexer)
        ?? OpenBrace.Lex(lexer)
        ?? OpenParenthesis.Lex(lexer)
        ?? OpenSquareBracket.Lex(lexer)
        ?? Percent.Lex(lexer)
        ?? Period.Lex(lexer)
        ?? Pipe.Lex(lexer)
        ?? Plus.Lex(lexer)
        ?? Pound.Lex(lexer)
        ?? Question.Lex(lexer)
        ?? Semicolon.Lex(lexer)        
        ?? Slash.Lex(lexer)
        ?? TextDelimiter.Lex(lexer)
        ?? Tilde.Lex(lexer) as Symbol;
}

internal class Punctuation : Symbol
{
    internal Punctuation(Lexer lexer, int length) : base(lexer, length) { }
}