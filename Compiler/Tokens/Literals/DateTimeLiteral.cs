using Ronin.Compiler;

namespace Ronin.Tokens.Literals;

internal class DateTimeLiteral : Token, ILexable<DateTimeLiteral>
{
    public DateTimeLiteral(Lexer lexer, int length) : base(lexer, length) { }

    public static DateTimeLiteral Lex(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span[lexer.Cursor..];
        if (span.IsEmpty) return null;

        if (span.Length is < 19) return null;

        if (!char.IsNumber(span[0])
            || !char.IsNumber(span[1])
            || !char.IsNumber(span[2])
            || !char.IsNumber(span[3])
            || span[4] is not '-'
            || !char.IsNumber(span[5])
            || !char.IsNumber(span[6])
            || span[7] is not '-'
            || !char.IsNumber(span[8])
            || !char.IsNumber(span[9])
            || !char.IsWhiteSpace(span[10])) return null;

        lexer.Cursor += 11;
        var time = TimeLiteral.Lex(lexer);
        lexer.Cursor -= 11 + (time?.Sourcecode.Length ?? 0);

        return time is null ? null : new DateTimeLiteral(lexer, 11 + time.Sourcecode.Length);
    }
}