using Ronin.Compiler;

namespace Ronin.Token.Value;

internal class Time : Literal
{
    internal Time(Lexer lexer, int length) : base(lexer, length) { }

    internal static new Lexeme Lex(Lexer lexer) => LexTwoDigitWithSpacedSuffixTimeLiteral(lexer)
        ?? LexTwoDigitWithUnspacedSuffixTimeLiteral(lexer)
        ?? LexTwoDigitWithoutSuffixTimeLiteral(lexer)
        ?? LexOneDigitWithSpacedSuffixTimeLiteral(lexer)
        ?? LexOneDigitWithUnspacedSuffixTimeLiteral(lexer) as Lexeme;

    private static Time LexTwoDigitWithSpacedSuffixTimeLiteral(Lexer lexer)
        => lexer.Length is < 10
        || !char.IsNumber(lexer[0])
        || !char.IsNumber(lexer[1])
        || lexer[2] is not ':'
        || !char.IsNumber(lexer[3])
        || !char.IsNumber(lexer[4])
        || lexer[5] is not ':'
        || !char.IsNumber(lexer[6])
        || !char.IsNumber(lexer[7])
        || !char.IsWhiteSpace(lexer[8])
        || lexer[9] is not 'a' and not 'A' and not 'p' and not 'P'
        ? null
        : new Time(lexer, 10);

    private static Time LexTwoDigitWithUnspacedSuffixTimeLiteral(Lexer lexer)
        => lexer.Length is < 9
        || !char.IsNumber(lexer[0])
        || !char.IsNumber(lexer[1])
        || lexer[2] is not ':'
        || !char.IsNumber(lexer[3])
        || !char.IsNumber(lexer[4])
        || lexer[5] is not ':'
        || !char.IsNumber(lexer[6])
        || !char.IsNumber(lexer[7])
        || lexer[8] is not 'a' and not 'A' and not 'p' and not 'P'
        ? null
        : new Time(lexer, 9);

    private static Time LexTwoDigitWithoutSuffixTimeLiteral(Lexer lexer)
        => lexer.Length is < 8
        || !char.IsNumber(lexer[0])
        || !char.IsNumber(lexer[1])
        || lexer[2] is not ':'
        || !char.IsNumber(lexer[3])
        || !char.IsNumber(lexer[4])
        || lexer[5] is not ':'
        || !char.IsNumber(lexer[6])
        || !char.IsNumber(lexer[7])
        ? null
        : new Time(lexer, 8);

    private static Time LexOneDigitWithSpacedSuffixTimeLiteral(Lexer lexer)
        => lexer.Length is < 9
        || !char.IsNumber(lexer[0])
        || lexer[1] is not ':'
        || !char.IsNumber(lexer[2])
        || !char.IsNumber(lexer[3])
        || lexer[4] is not ':'
        || !char.IsNumber(lexer[5])
        || !char.IsNumber(lexer[6])
        || !char.IsWhiteSpace(lexer[7])
        || lexer[8] is not 'a' and not 'A' and not 'p' and not 'P'
        ? null
        : new Time(lexer, 9);

    private static Time LexOneDigitWithUnspacedSuffixTimeLiteral(Lexer lexer)
        => lexer.Length is < 8
        || !char.IsNumber(lexer[0])
        || lexer[1] is not ':'
        || !char.IsNumber(lexer[2])
        || !char.IsNumber(lexer[3])
        || lexer[4] is not ':'
        || !char.IsNumber(lexer[5])
        || !char.IsNumber(lexer[6])
        || lexer[7] is not 'a' and not 'A' and not 'p' and not 'P'
        ? null
        : new Time(lexer, 8);
}
