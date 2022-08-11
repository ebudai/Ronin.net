using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Ronin.Parser.Grammar;

[DebuggerDisplay("{Value}")]
internal class Literal : Syntax
{
    internal string Value { get; init; }
    internal string Datatype { get; init; }

    internal new static Literal Parse(Context context)
    {
        return ParseText(textliteral, Primitive.text, context)
            ?? ParseText(charliteral, Primitive.character, context)
            ?? ParseText(unicodeliteral, Primitive.character, context)
            ?? ParseHex(context)
            ?? ParseBinary(context)
            ?? ParseDecimal(halfliteral, Primitive.dec16, context)
            ?? ParseDecimal(doubleliteral, Primitive.dec64, context)
            ?? ParseDecimal(decimalliteral, Primitive.@decimal, context)
            ?? ParseDecimal(moneyliteral, Primitive.money, context)
            ?? ParseInteger(context);
    }

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
            Datatype = value <= byte.MaxValue ? Primitive.@byte
                : value <= ushort.MaxValue ? Primitive.bits16
                : value <= uint.MaxValue ? Primitive.bits32
                : value <= ulong.MaxValue ? Primitive.bits64
                : Primitive.bitlist
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
                <= 8 => Primitive.@byte,
                <= 16 => Primitive.bits16,
                <= 32 => Primitive.bits32,
                <= 64 => Primitive.bits64,
                _ => Primitive.bitlist
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
            Datatype = literal.EndsWith("i8", StringComparison.OrdinalIgnoreCase) ? Primitive.int8
                : literal.EndsWith("i16", StringComparison.OrdinalIgnoreCase) ? Primitive.int16
                : literal.EndsWith("i64", StringComparison.OrdinalIgnoreCase) ? Primitive.int64
                : value <= long.MinValue || value >= long.MaxValue ? Primitive.bigint
                : value <= int.MinValue || value >= int.MaxValue ? Primitive.int64
                : Primitive.integer
        };
    }
}
