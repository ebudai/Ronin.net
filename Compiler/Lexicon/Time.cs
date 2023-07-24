// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Time : Literal
{
    public static new Token Lex(ref Lexer lexer) => LexTwoDigitWithSpacedSuffixTimeLiteral(ref lexer)
        ?? LexTwoDigitWithUnspacedSuffixTimeLiteral(ref lexer)
        ?? LexTwoDigitWithoutSuffixTimeLiteral(ref lexer)
        ?? LexOneDigitWithSpacedSuffixTimeLiteral(ref lexer)
        ?? LexOneDigitWithUnspacedSuffixTimeLiteral(ref lexer) as Token;

    private static Time LexTwoDigitWithSpacedSuffixTimeLiteral(ref Lexer lexer)
        => lexer.Length is < 10
        || !char.IsDigit(lexer[0])
        || !char.IsDigit(lexer[1])
        || lexer[2] is not ':'
        || !char.IsDigit(lexer[3])
        || !char.IsDigit(lexer[4])
        || lexer[5] is not ':'
        || !char.IsDigit(lexer[6])
        || !char.IsDigit(lexer[7])
        || !char.IsWhiteSpace(lexer[8])
        || lexer[9] is not 'a' and not 'A' and not 'p' and not 'P'
        ? null
        : new Time { Memory = lexer.Commit(10) };

    private static Time LexTwoDigitWithUnspacedSuffixTimeLiteral(ref Lexer lexer)
        => lexer.Length is < 9
        || !char.IsDigit(lexer[0])
        || !char.IsDigit(lexer[1])
        || lexer[2] is not ':'
        || !char.IsDigit(lexer[3])
        || !char.IsDigit(lexer[4])
        || lexer[5] is not ':'
        || !char.IsDigit(lexer[6])
        || !char.IsDigit(lexer[7])
        || lexer[8] is not 'a' and not 'A' and not 'p' and not 'P'
        ? null
        : new Time { Memory = lexer.Commit(9) };

    private static Time LexTwoDigitWithoutSuffixTimeLiteral(ref Lexer lexer)
        => lexer.Length is < 8
        || !char.IsDigit(lexer[0])
        || !char.IsDigit(lexer[1])
        || lexer[2] is not ':'
        || !char.IsDigit(lexer[3])
        || !char.IsDigit(lexer[4])
        || lexer[5] is not ':'
        || !char.IsDigit(lexer[6])
        || !char.IsDigit(lexer[7])
        ? null
        : new Time { Memory = lexer.Commit(8) };

    private static Time LexOneDigitWithSpacedSuffixTimeLiteral(ref Lexer lexer)
        => lexer.Length is < 9
        || !char.IsDigit(lexer[0])
        || lexer[1] is not ':'
        || !char.IsDigit(lexer[2])
        || !char.IsDigit(lexer[3])
        || lexer[4] is not ':'
        || !char.IsDigit(lexer[5])
        || !char.IsDigit(lexer[6])
        || !char.IsWhiteSpace(lexer[7])
        || lexer[8] is not 'a' and not 'A' and not 'p' and not 'P'
        ? null
        : new Time { Memory = lexer.Commit(9) };

    private static Time LexOneDigitWithUnspacedSuffixTimeLiteral(ref Lexer lexer)
        => lexer.Length is < 8
        || !char.IsDigit(lexer[0])
        || lexer[1] is not ':'
        || !char.IsDigit(lexer[2])
        || !char.IsDigit(lexer[3])
        || lexer[4] is not ':'
        || !char.IsDigit(lexer[5])
        || !char.IsDigit(lexer[6])
        || lexer[7] is not 'a' and not 'A' and not 'p' and not 'P'
        ? null
        : new Time { Memory = lexer.Commit(8) };
}
