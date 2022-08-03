using System.Text;
using System.Text.RegularExpressions;

namespace Ronin.Builder;

internal static class Language
{
    public static class Primitives
    {
        public const string nothing = nameof(nothing);
        public const string something = nameof(something);
        public const string anything = nameof(anything);

        public const string number = nameof(number);

        public const string integer = nameof(integer);
        public const string int8 = nameof(int8);
        public const string int16 = nameof(int16);
        public const string int64 = nameof(int64);
        public const string bigint = nameof(bigint);

        public const string @byte = nameof(@byte);
        public const string bits16 = nameof(bits16);
        public const string bits32 = nameof(bits32);
        public const string bits64 = nameof(bits64);
        public const string bitlist = nameof(bitlist);

        public const string dec16 = nameof(dec16);
        public const string @decimal = nameof(@decimal);
        public const string dec64 = nameof(dec64);
        public const string rational = nameof(rational);

        public const string money = nameof(money);

        public const string character = nameof(character);
        public const string text = nameof(text);

        public const string maybe = nameof(maybe);

        public const string date = nameof(date);
        public const string time = nameof(time);
        public const string datetime = nameof(datetime);
    }

    public static class Symbols
    {
        public const string Terminal = ".";
        public const string Separator = ",";

        public const string Aggregates = "()";
        public const string Lists = "[]";
        public const string Scopes = "{}";
    }

    /*public static bool IsHexLiteral(this Match match) => match.Success
        && match.ValueSpan.Length >= 3
        && match.ValueSpan[0] == '0'
        && (match.ValueSpan[1] == 'x' || match.ValueSpan[1] == 'X');

    public static bool IsBinaryLiteral(this Match match) => match.Success
        && match.ValueSpan.Length >= 3
        && match.ValueSpan[0] == '0'
        && (match.ValueSpan[1] == 'b' || match.ValueSpan[1] == 'B');

    public static bool IsHalfLiteral(this Match match) => match.Success
        && match.ValueSpan.Length >= 5
        && (match.ValueSpan[^3] == 'd' || match.ValueSpan[^3] == 'D')
        && match.ValueSpan.EndsWith("16");

    public static bool IsDoubleLiteral(this Match match) => match.Success
        && match.ValueSpan.Length >= 5
        && (match.ValueSpan[^3] == 'd' || match.ValueSpan[^3] == 'D')
        && match.ValueSpan.EndsWith("64");

    public static bool IsDecimalLiteral(this Match match) => match.Success
        && match.ValueSpan.Length >= 3
        && match.ValueSpan.Contains('.')
        && Rune.IsDigit(Rune.GetRuneAt(match.Value, 0))
        && Rune.IsDigit(Rune.GetRuneAt(match.Value, match.Value.Length - 1));*/
}


