using Ronin.Compiler;

namespace Ronin.Tokens.Literals;

internal class DateTimeLiteral : Token, ILexable<DateTimeLiteral>
{
    public DateTimeLiteral(Lexer lexer, int length) : base(lexer, length) { }

    public static DateTimeLiteral Lex(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span;
        if (span.IsEmpty) return null;

        if (span.Length is < 20) return null;

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
        if (!char.IsWhiteSpace(span[10])) return null;
        if (!char.IsNumber(span[11])) return null;
        if (!char.IsNumber(span[12])) return null;
        if (span[13] is not ':') return null;
        if (!char.IsNumber(span[14])) return null;
        if (!char.IsNumber(span[15])) return null;
        if (span[16] is not ':') return null;
        if (!char.IsNumber(span[17])) return null;
        if (!char.IsNumber(span[18])) return null;
        if (span[19] is not 'a' and not 'A' and not 'p' and not 'P') return null;

        return new DateTimeLiteral(lexer, 20);
    }
}