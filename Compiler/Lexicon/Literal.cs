// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using System.Text.RegularExpressions;

namespace Ronin.Lexicon;

internal class Literal : Token
{
    public static Literal Lex(ref Lexer lexer)
        => Date.Lex(ref lexer)
        ?? Numeric.Lex(ref lexer)
        ?? Text.Lex(ref lexer) as Literal;
}

internal class Date : Literal
{
    public static new Date Lex(ref Lexer lexer)
    {
        if (lexer.Length is < Length) return null;

        //TODO allow year to be one or more digits
        if (char.IsDigit(lexer[0]) is not true) return null;
        if (char.IsDigit(lexer[1]) is not true) return null;
        if (char.IsDigit(lexer[2]) is not true) return null;
        if (char.IsDigit(lexer[3]) is not true) return null;
        if (lexer[4] is not '-') return null;
        if (char.IsDigit(lexer[5]) is not true) return null;
        if (char.IsDigit(lexer[6]) is not true) return null;
        if (lexer[7] is not '-') return null;
        if (char.IsDigit(lexer[8]) is not true) return null;
        if (char.IsDigit(lexer[9]) is not true) return null;

        return new Date { Memory = lexer.AdvanceBy(Length) };
    }

    private const int Length = 10;
}

internal partial class Numeric : Literal
{
    public static new Numeric Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || char.IsDigit(lexer[0]) is false) return null;

        int length = 1;
        for (int max = lexer.Length; length != max; ++length)
        {
            char c = lexer[length];

            if (char.IsWhiteSpace(c)) break;
            if (char.IsDigit(c) is false && c is not ',' and not '.') break;
        }

        var number = lexer[..length].ToString();

        var match = NumbersWithCommas().Match(number);
        if (match.Success) return new Numeric { Memory = lexer.AdvanceBy(match.Length) };

        match = NumbersWithoutCommas().Match(number);
        return new Numeric { Memory = lexer.AdvanceBy(match.Length) };
    }

    [GeneratedRegex("[0-9]+([.][0-9]+)?", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex NumbersWithoutCommas();

    [GeneratedRegex("[0-9]{1,3}(,[0-9]{3})+([.][0-9]+)?", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex NumbersWithCommas();
}

internal class Text : Literal
{
    public static new Text Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not TextDelimiter.symbol) return null;

        var escaped = false;

        for (var i = 1; i < lexer.Length; ++i)
        {
            // Counting the run matters: «\\» is an escaped backslash, so the quote
            // after it closes the text. Looking only at the previous character
            // read that as an escaped quote and ran on to the next one.
            if (escaped) { escaped = false; continue; }
            if (lexer[i] is '\\') { escaped = true; continue; }
            if (lexer[i] is TextDelimiter.symbol) return new Text { Memory = lexer.AdvanceBy(i + 1) };
        }

        return null;
    }
}