using Ronin.Compiler;

namespace Ronin.Tokens.Literals;

internal class TimeLiteral : Token, ILexable<TimeLiteral>
{
    public TimeLiteral(Lexer lexer, int length) : base(lexer, length) { }

    public static TimeLiteral Lex(Lexer lexer)
        => LexTwoDigitWithSpacedSuffix(lexer)
        ?? LexTwoDigitWithUnspacedSuffix(lexer)
        ?? LexTwoDigitWithoutSuffix(lexer)
        ?? LexOneDigitWithSpacedSuffix(lexer)
        ?? LexOneDigitWithUnspacedSuffix(lexer);

    private static TimeLiteral LexTwoDigitWithSpacedSuffix(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span;

        return span.IsEmpty
            || span.Length is < 10
            || !char.IsNumber(span[0])
            || !char.IsNumber(span[1])
            || span[2] is not ':'
            || !char.IsNumber(span[3])
            || !char.IsNumber(span[4])
            || span[5] is not ':'
            || !char.IsNumber(span[6])
            || !char.IsNumber(span[7])
            || !char.IsWhiteSpace(span[8])
            || span[9] is not 'a' and not 'A' and not 'p' and not 'P'
            ? null
            : new TimeLiteral(lexer, 10);
    }

    private static TimeLiteral LexTwoDigitWithUnspacedSuffix(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span;

        return span.IsEmpty
            || span.Length is < 9
            || !char.IsNumber(span[0])
            || !char.IsNumber(span[1])
            || span[2] is not ':'
            || !char.IsNumber(span[3])
            || !char.IsNumber(span[4])
            || span[5] is not ':'
            || !char.IsNumber(span[6])
            || !char.IsNumber(span[7])
            || span[8] is not 'a' and not 'A' and not 'p' and not 'P'
            ? null
            : new TimeLiteral(lexer, 9);
    }

    private static TimeLiteral LexTwoDigitWithoutSuffix(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span;

        return span.IsEmpty
            || span.Length is < 8
            || !char.IsNumber(span[0])
            || !char.IsNumber(span[1])
            || span[2] is not ':'
            || !char.IsNumber(span[3])
            || !char.IsNumber(span[4])
            || span[5] is not ':'
            || !char.IsNumber(span[6])
            || !char.IsNumber(span[7])
            ? null
            : new TimeLiteral(lexer, 8);
    }

    private static TimeLiteral LexOneDigitWithSpacedSuffix(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span;

        return span.IsEmpty
            || span.Length is < 9
            || !char.IsNumber(span[0])
            || span[1] is not ':'
            || !char.IsNumber(span[2])
            || !char.IsNumber(span[3])
            || span[4] is not ':'
            || !char.IsNumber(span[5])
            || !char.IsNumber(span[6])
            || !char.IsWhiteSpace(span[7])
            || span[8] is not 'a' and not 'A' and not 'p' and not 'P'
            ? null
            : new TimeLiteral(lexer, 9);
    }

    private static TimeLiteral LexOneDigitWithUnspacedSuffix(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span;

        return span.IsEmpty
            || span.Length is < 8
            || !char.IsNumber(span[0])
            || span[1] is not ':'
            || !char.IsNumber(span[2])
            || !char.IsNumber(span[3])
            || span[4] is not ':'
            || !char.IsNumber(span[5])
            || !char.IsNumber(span[6])
            || span[7] is not 'a' and not 'A' and not 'p' and not 'P'
            ? null
            : new TimeLiteral(lexer, 8);
    }
}