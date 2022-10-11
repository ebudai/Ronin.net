using Ronin.Compiler;
using Ronin.Token.Value;

namespace Ronin.Token;

internal class Literal : Lexeme
{
    internal Literal(Lexer lexer, int length) : base(lexer, length) { }

    internal static Lexeme Lex(Lexer lexer)
        => Binary.Lex(lexer)
        ?? Character.Lex(lexer)
        ?? Date.Lex(lexer)
        ?? Hex.Lex(lexer)
        ?? Time.Lex(lexer)
        ?? Integer.Lex(lexer)
        ?? Money.Lex(lexer)
        ?? Number.Lex(lexer)
        ?? Text.Lex(lexer)
        ?? Url.Lex(lexer);
}
