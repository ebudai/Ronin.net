using Ronin.Compiler;

namespace Ronin.Tokens.Literals;

internal class DateLiteral : Token, ILexable<DateLiteral>
{
    public DateLiteral(Lexer lexer, int length) : base(lexer, length) { }

    public static DateLiteral Lex(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;

        if (lexer.Length is < 10) return null;

        if (!char.IsNumber(lexer[0])) return null;
        if (!char.IsNumber(lexer[1])) return null;
        if (!char.IsNumber(lexer[2])) return null;
        if (!char.IsNumber(lexer[3])) return null;
        if (lexer[4] is not '-') return null;
        if (!char.IsNumber(lexer[5])) return null;
        if (!char.IsNumber(lexer[6])) return null;
        if (lexer[7] is not '-') return null;
        if (!char.IsNumber(lexer[8])) return null;
        if (!char.IsNumber(lexer[9])) return null;

        return new DateLiteral(lexer, 10);
    }
}
