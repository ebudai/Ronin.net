using Ronin.Grammar;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

using static Ronin.Parser.Form;

namespace Ronin.Parser;

internal static class LiteralParser
{
    internal static Literal Parse(Context context)
        => ParseText(textliteral, Scalar.text, context)
        ?? ParseText(charliteral, Scalar.character, context)
        ?? ParseText(unicodeliteral, Scalar.character, context)
        ?? ParseHex(context)
        ?? ParseBinary(context)
        ?? ParseDecimal(numberliteral, Scalar.number, context)
        ?? ParseDecimal(moneyliteral, Scalar.money, context)
        ?? ParseInteger(context);

    private static Literal ParseText(Regex regex, string primitive, Context context)
    {
        var lexed = context.Lex(regex);
        return lexed is null ? null : new Literal { Value = lexed, Datatype = primitive };
    }

    private static Literal ParseHex(Context context)
    {
        var literal = context.Lex(hexliteral)?.Replace("_", "")[hexprefix.Length..];
        if (literal is null) return null;

        var parsed = literal.Length is 1 ? '0' + literal : literal;

        if (!BigInteger.TryParse(parsed, NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out var value))
        {
            throw new Exception($"{literal} matched hex literal but BigInteger.TryParse() failed"); // this should never happen
        }

        return new Literal
        {
            Value = literal,
            Datatype = value <= byte.MaxValue ? Scalar.@byte
                : value <= ushort.MaxValue ? Scalar.bits16
                : value <= uint.MaxValue ? Scalar.bits32
                : value <= ulong.MaxValue ? Scalar.bits64
                : Scalar.bitlist
        };
    }

    private static Literal ParseBinary(Context context)
    {
        var lexed = context.Lex(binaryliteral)?.Replace("_", "")[binaryprefix.Length..];
        return lexed is null ? null : new Literal
        {
            Value = lexed,
            Datatype = lexed.Length switch
            {
                <= 8 => Scalar.@byte,
                <= 16 => Scalar.bits16,
                <= 32 => Scalar.bits32,
                <= 64 => Scalar.bits64,
                _ => Scalar.bitlist
            }
        };
    }

    private static Literal ParseDecimal(Regex regex, string primitive, Context context)
    {
        var lexed = context.Lex(regex)?.Replace("_", "");
        return lexed is null ? null : new Literal { Value = lexed, Datatype = primitive };
    }

    private static Literal ParseInteger(Context context)
    {
        NumberStyles numberstyle = NumberStyles.None; //TODO investigate using , for digit separator (in 3's) instead of _ whereever       NumberStyles.AllowThousands

        var literal = context.Lex(integerliteral)?.Replace("_", "");
        if (literal is null) return null;

        var index = literal.IndexOf('i', StringComparison.OrdinalIgnoreCase);
        var parsed = index is -1 ? literal : literal[..index];
        if (!BigInteger.TryParse(parsed.TrimEnd(), numberstyle, CultureInfo.CurrentCulture, out var value))
        {
            throw new Exception($"{literal} matched integer literal but BigInteger.TryParse() failed"); // this should never happen
        }

        return new Literal
        {
            Value = literal,
            Datatype = literal.EndsWith("i8", StringComparison.OrdinalIgnoreCase) ? Scalar.int8
                : literal.EndsWith("i16", StringComparison.OrdinalIgnoreCase) ? Scalar.int16
                : literal.EndsWith("i64", StringComparison.OrdinalIgnoreCase) ? Scalar.int64
                : value <= long.MinValue || value >= long.MaxValue ? Scalar.bigint
                : value <= int.MinValue || value >= int.MaxValue ? Scalar.int64
                : Scalar.integer
        };
    }
}
