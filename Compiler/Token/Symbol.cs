using Ronin.Compiler;
using Ronin.Token.Delimiter;

namespace Ronin.Token;

internal class Symbol : Lexeme
{
    internal Symbol(Lexer lexer, int length) : base(lexer, length) { }

    internal static bool IsSymbol(Lexer lexer, int i = 0)
    {
        var text = lexer[i..].Span;
        if (text.IsEmpty) return false;
        return text.StartsWith(Returns.character)
            || text.StartsWith(Comment.singleline)
            || text.StartsWith(Comment.multilinestart)
            || text[0] is Assign.character
            or CharacterDelimiter.character
            or CloseBrace.character
            or CloseParenthesis.character
            or CloseSquareBracket.character
            or OpenBrace.character
            or OpenParenthesis.character
            or OpenSquareBracket.character
            or Separator.character
            or Terminal.character
            or TextDelimiter.character;
    }

    internal static bool IsNonTerminalSymbol(Lexer lexer, int i = 0) => IsSymbol(lexer, i) && lexer[i] is not Terminal.character;

    internal static Symbol Lex(Lexer lexer)
        => Returns.Lex(lexer)
        ?? CharacterDelimiter.Lex(lexer)
        ?? CloseBrace.Lex(lexer)
        ?? CloseParenthesis.Lex(lexer)
        ?? CloseSquareBracket.Lex(lexer)
        ?? OpenBrace.Lex(lexer)
        ?? OpenParenthesis.Lex(lexer)
        ?? OpenSquareBracket.Lex(lexer)
        ?? Assign.Lex(lexer)
        ?? Separator.Lex(lexer)
        ?? Terminal.Lex(lexer)
        ?? TextDelimiter.Lex(lexer) as Symbol;
}
