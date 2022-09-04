using Ronin.Compiler;

namespace Ronin.Tokens.Literals;

internal class DateTimeLiteral : Token, ILexable<DateTimeLiteral>
{
    public DateTimeLiteral(Lexer lexer, int length) : base(lexer, length) { }

    public static DateTimeLiteral Lex(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;

        if (lexer.Length is < 19) return null;

        if (!char.IsNumber(lexer[0])
            || !char.IsNumber(lexer[1])
            || !char.IsNumber(lexer[2])
            || !char.IsNumber(lexer[3])
            || lexer[4] is not '-'
            || !char.IsNumber(lexer[5])
            || !char.IsNumber(lexer[6])
            || lexer[7] is not '-'
            || !char.IsNumber(lexer[8])
            || !char.IsNumber(lexer[9])
            || !char.IsWhiteSpace(lexer[10])) return null;

        lexer.Cursor += 11;
        var time = TimeLiteral.Lex(lexer);
        lexer.Cursor -= 11 + (time?.Sourcecode.Length ?? 0);

        return time is null ? null : new DateTimeLiteral(lexer, 11 + time.Sourcecode.Length);
    }
}