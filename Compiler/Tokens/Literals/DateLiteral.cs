using Ronin.Compiler;

namespace Ronin.Tokens.Literals;

internal class DateLiteral : Token, ILexable<DateLiteral>
{
    public DateLiteral(Lexer lexer, int length) : base(lexer, length) { }

    public static DateLiteral Lex(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span[lexer.Cursor..];
        if (span.IsEmpty) return null;

        if (span.Length is < 10) return null;

        if (!char.IsNumber(span[0])) return null;
        if (!char.IsNumber(span[1])) return null;
        if (!char.IsNumber(span[2])) return null;
        if (!char.IsNumber(span[3])) return null;
        if (span[4] is not '-') return null;
        if (!char.IsNumber(span[5])) return null;
        if (!char.IsNumber(span[6])) return null;
        if (span[7] is not '-') return null;
        if (!char.IsNumber(span[8])) return null;
        if (!char.IsNumber(span[9])) return null;

        return new DateLiteral(lexer, 10);
    }
}
