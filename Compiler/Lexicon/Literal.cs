// Copyright © 2023 Eric Budai

using Ronin.Compiler;

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

/// <summary>
///     A number, integer or decimal, with commas allowed as digit separators.
/// </summary>
///
/// <remarks>
///     <para>
///     A comma is a digit separator only where it sits <em>directly</em> between
///     digits. «1,234» is one number and «1, 234» is two of something — which is
///     how the two are already written by hand, so the rule makes the reader
///     right rather than asking them to learn anything. The companion rule lives
///     in <see cref="Separator"/>: a separator must be followed by a space, so
///     the unspaced form is always a number and never a list.
///     </para>
///     <para>
///     Groups must be well formed — first one to three digits, every later one
///     exactly three — and the longest well-formed prefix wins. A bare run of
///     digits is always a number however long it is; the group rule applies only
///     once a comma has appeared.
///     </para>
/// </remarks>
internal class Numeric : Literal
{
    /// <summary>Whether it carries a fractional part, which is purely lexical.</summary>
    public bool IsDecimal { get; private init; }

    public static new Numeric Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || char.IsDigit(lexer[0]) is false) return null;

        var length = Digits(lexer, 0);
        var isdecimal = false;

        // a fractional part takes no separators: «1,234.567890» is one group
        if (length + 1 < lexer.Length && lexer[length] is '.' && char.IsDigit(lexer[length + 1]))
        {
            var fraction = 1;
            while (length + fraction < lexer.Length && char.IsDigit(lexer[length + fraction])) ++fraction;

            length += fraction;
            isdecimal = true;
        }

        return new Numeric { IsDecimal = isdecimal, Memory = lexer.AdvanceBy(length) };
    }

    /// <summary>
    ///     The length of the longest well-formed digit run at
    ///     <paramref name="from"/>, commas included.
    /// </summary>
    /// <remarks>
    ///     The caller has already established that <paramref name="from"/> is a
    ///     digit, and a run of digits with no comma in it is always well formed —
    ///     so shrinking always terminates on one and there is no failure case to
    ///     carry.
    /// </remarks>
    private static int Digits(in Lexer lexer, int from)
    {
        var run = from;
        while (run < lexer.Length && (char.IsDigit(lexer[run]) || lexer[run] is ',')) ++run;

        // a trailing comma is never part of a number
        while (lexer[run - 1] is ',') --run;

        // drop the last group and retry, until what is left is well formed
        while (Grouped(lexer, from, run) is false)
        {
            --run;
            while (lexer[run] is not ',') --run;
        }

        return run - from;
    }

    /// <summary>First group one to three digits, every later group exactly three.</summary>
    private static bool Grouped(in Lexer lexer, int from, int to)
    {
        var group = 0;
        var first = true;

        for (var i = from; i != to; ++i)
        {
            if (lexer[i] is not ',')
            {
                ++group;
                continue;
            }

            // a bare run of digits is a number however long, so the size rule
            // only starts applying once a comma has appeared
            if (group is 0 || (first ? group > 3 : group is not 3)) return false;

            first = false;
            group = 0;
        }

        return first ? group is not 0 : group is 3;
    }
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
