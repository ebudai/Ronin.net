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

/// <summary>
///     A calendar date, «year-month-day». The year is four or more digits; month
///     and day are two digits each.
/// </summary>
///
/// <remarks>
///     <para>
///     The year field is four wide at minimum because it is the only field a
///     reader can identify by looking at it — month and day are always two. So
///     «2026-08-16» reads as a date and «01-02-03» is not one: a one-digit year
///     would give «01-02-03» three readings across the world with no cue, which is
///     the hazard the language refuses everywhere else. Year 5 is «0005-01-01», the
///     ISO spelling, so the minimum costs no expressiveness. There is no maximum
///     but the type's own «0 .. 2^57», so the year is the <em>longest</em> run of
///     digits: «12345-01-01» is year 12345, not a four-digit match that falls back
///     to «12345 - 01 - 01».
///     </para>
///     <para>
///     The year is spelled in digits, not as a <see cref="Numeric"/>: no digit
///     grouping, so «1,234-01-01» is not a date, and no sign, so a «-» directly
///     before a date is always the operator. Shape alone decides the token — an
///     out-of-range field like «2026-13-01» still lexes as a date and is left for a
///     later range check to find, because a literal must not change kind by its own
///     value.
///     </para>
/// </remarks>
internal class Date : Literal
{
    public static new Date Lex(ref Lexer lexer)
    {
        var year = 0;
        while (year < lexer.Length && char.IsDigit(lexer[year])) ++year;

        if (year < Minimum) return null;
        if (year + Tail > lexer.Length) return null;

        if (lexer[year + 0] is not '-') return null;
        if (char.IsDigit(lexer[year + 1]) is not true) return null;
        if (char.IsDigit(lexer[year + 2]) is not true) return null;
        if (lexer[year + 3] is not '-') return null;
        if (char.IsDigit(lexer[year + 4]) is not true) return null;
        if (char.IsDigit(lexer[year + 5]) is not true) return null;

        return new Date { Memory = lexer.AdvanceBy(year + Tail) };
    }

    private const int Minimum = 4; // digits — the year is four wide or more, so it labels itself
    private const int Tail = 6;    // «-dd-dd» — month and day, each exactly two digits
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
        if (lexer.IsEmpty || Digit(lexer[0]) is false) return null;

        var length = Digits(lexer, 0);
        var isdecimal = false;

        // a fractional part takes no separators: «1,234.567890» is one group
        if (length + 1 < lexer.Length && lexer[length] is '.' && Digit(lexer[length + 1]))
        {
            var fraction = 1;
            while (length + fraction < lexer.Length && Digit(lexer[length + fraction])) ++fraction;

            length += fraction;
            isdecimal = true;
        }

        return new Numeric { IsDecimal = isdecimal, Memory = lexer.AdvanceBy(length) };
    }

    /// <summary>
    ///     An ASCII decimal digit. The source alphabet is «0-9» and nothing wider
    ///     (NUMERALALPHABET): «char.IsDigit» admits every Unicode decimal digit — «١» among
    ///     hundreds — which mix across scripts and hide lookalikes, so a numeral could read as
    ///     one value and be another, the opposite of what this language trades for. The lexer is
    ///     the authority for the alphabet; a run outside «0-9» is simply not a number here.
    ///     <para>
    ///     «internal» because «Word» reads it too: a word may not START where a number does,
    ///     and "where a number starts" is exactly this — so a Unicode digit «char.IsDigit» would
    ///     have taken is not a number AND may begin a name, rather than being a run no token
    ///     consumes, which hangs the lexer loop.
    ///     </para>
    /// </summary>
    internal static bool Digit(char c) => c is >= '0' and <= '9';

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
        while (run < lexer.Length && (Digit(lexer[run]) || lexer[run] is ',')) ++run;

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
