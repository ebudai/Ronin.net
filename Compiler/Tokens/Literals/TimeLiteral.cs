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
        : new TimeLiteral(lexer, 10);

    private static TimeLiteral LexTwoDigitWithUnspacedSuffix(Lexer lexer) 
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
        : new TimeLiteral(lexer, 9);    

    private static TimeLiteral LexTwoDigitWithoutSuffix(Lexer lexer) 
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
        : new TimeLiteral(lexer, 8);

    private static TimeLiteral LexOneDigitWithSpacedSuffix(Lexer lexer) 
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
        : new TimeLiteral(lexer, 9);    

    private static TimeLiteral LexOneDigitWithUnspacedSuffix(Lexer lexer) 
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
        : new TimeLiteral(lexer, 8);
}