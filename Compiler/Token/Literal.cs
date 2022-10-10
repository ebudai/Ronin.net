using Ronin.Compiler;
using static Ronin.Token.Literal.Kind;

namespace Ronin.Token;

internal class Literal : Lexeme
{
    internal Literal(Lexer lexer, int length, Kind kind) : base(lexer, length) => LiteralKind = kind;

    internal Kind LiteralKind { get; }

    internal static Lexeme Lex(Lexer lexer)
        => LexBinaryLiteral(lexer)
        ?? LexCharacterLiteral(lexer)
        ?? LexDateLiteral(lexer)
        ?? LexHexLiteral(lexer)
        ?? LexTimeLiteral(lexer)
        ?? LexIntegerLiteral(lexer)
        ?? LexMoneyLiteral(lexer)
        ?? LexNumberLiteral(lexer)
        ?? LexTextLiteral(lexer)        
        ?? LexUrlLiteral(lexer);

    internal enum Kind
    {
        binary,
        character,
        date,
        hex,
        integer,
        money,
        number,
        text,
        time,
        url
    }

    private static Lexeme LexBinaryLiteral(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;
        if (lexer[0] is not '0' || lexer[1] is not 'b' and not 'B') return null;

        if (lexer.Length is <= 2) return new Error(lexer, lexer.Length, "unterminated binary literal");

        int length = 2;
        for (int i = 2, max = lexer.Length; i != max; ++i)
        {
            if (lexer[i] is '0' or '1' or '_')
            {
                ++length;
                continue;
            }

            if (char.IsWhiteSpace(lexer[i]) || Symbol.IsSymbol(lexer, i)/* || lexer[i] is Symbol.terminal*/)
            {
                length = i;
                break;
            }

            return new Error(lexer, length, $"invalid char '{lexer[i]}' at {i} for binary literal");
        }

        return new Literal(lexer, length, binary);
    }

    private static Lexeme LexCharacterLiteral(Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not '\'') return null;

        var length = lexer[1..].Span.IndexOf('\'');

        if (length is < 0) return new Error(lexer, lexer.Length, "unterminated character literal");

        if (length is 0) return new Error(lexer, 2, "empty character literal");

        if (length is not 1 and not 6) return new Error(lexer, length + 2, "bad unicode literal");

        if (length is 6)
        {
            for (var i = 3; i != length; ++i)
            {
                if (!char.IsNumber(lexer[i]) && lexer[i] is not 'A' and not 'a' and not 'B' and not 'C' and not 'c' and not 'D' and not 'd' and not 'E' and not 'e' and not 'F' and not 'f')
                {
                    return new Error(lexer, i, $"invalid character '{lexer[i]}' at {i} for unichar literal");
                }
            }
        }
        
        return new Literal(lexer, length + 2, character);
    }

    private static Literal LexDateLiteral(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;

        if (lexer.Length is < 10) return null;

        if (!char.IsNumber(lexer[0])) return null;
        if (!char.IsNumber(lexer[1])) return null;
        if (!char.IsNumber(lexer[2])) return null;
        if (!char.IsNumber(lexer[3])) return null;
        if (lexer[4] is not '-') return null;
        if (!char.IsNumber(lexer[5])) return null;
        if (!char.IsNumber(lexer[6])) return null;
        if (lexer[7] is not '-') return null;
        if (!char.IsNumber(lexer[8])) return null;
        if (!char.IsNumber(lexer[9])) return null;

        return new Literal(lexer, 10, date);
    }

    private static Lexeme LexHexLiteral(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;
        if (lexer[0] is not '0' || lexer[1] is not 'x' and not 'X') return null;

        if (lexer.Length is <= 2) return new Error(lexer, lexer.Length, "unterminated hex literal");

        int length = 2;
        for (int i = 2, max = lexer.Length; i != max; ++i)
        {
            if (char.IsWhiteSpace(lexer[i]) || Symbol.IsSymbol(lexer, i))
            {
                length = i;
                break;
            }

            if (!char.IsNumber(lexer[i]) && lexer[i] is not 'A' and not 'a' and not 'B' and not 'b' and not 'C' and not 'c' and not 'D' and not 'd' and not 'E' and not 'e' and not 'F' and not 'f' and not '_')
            {
                return new Error(lexer, i, $"invalid character '{lexer[i]}' at {i} for hex literal");
            }

            ++length;
        }
        return new Literal(lexer, length, hex);
    }

    private static Lexeme LexIntegerLiteral(Lexer lexer)
    {
        if (lexer.IsEmpty || !char.IsNumber(lexer[0])) return null;

        int length = 0;
        for (int i = 0, max = lexer.Length; i != max; ++i)
        {
            if (lexer[i] is '.') return null;

            if (char.IsWhiteSpace(lexer[i]) || Symbol.IsSymbol(lexer, i))
            {
                length = i;
                break;
            }

            if (!char.IsNumber(lexer[i]) && lexer[i] is not '_') return new Error(lexer, i, $"integer literal with non-numeric character '{lexer[i]}' at {i}");

            ++length;
        }

        return new Literal(lexer, length, integer);
    }

    private static Lexeme LexMoneyLiteral(Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not '$') return null;

        if (lexer.Length is < 2) return new Error(lexer, lexer.Length, "unterminated money literal");

        if (!char.IsNumber(lexer[1])) return null;

        int length = 2;
        bool hasPeriod = false;
        for (int i = 2, max = lexer.Length; i != max; ++i)
        {
            if (char.IsWhiteSpace(lexer[i]) || Symbol.IsSymbol(lexer, i))
            {
                length = i;
                break;
            }

            if (!char.IsNumber(lexer[i]) && lexer[i] is not '_' and not '.') return new Error(lexer, i, $"money literal with non-numeric character '{lexer[i]}' at {i}");

            if (lexer[i] is '.')
            {
                if (hasPeriod) return new Error(lexer, i, "money literal with multiple dots");
                hasPeriod = true;
            }

            ++length;
        }

        if (lexer[length - 1] is '.') return new Error(lexer, length - 1, "money literal cannot end with a dot");

        return new Literal(lexer, length, money);
    }

    private static Lexeme LexNumberLiteral(Lexer lexer)
    {
        if (lexer.IsEmpty || !char.IsNumber(lexer[0])) return null;

        if (lexer.Length is < 3) return new Error(lexer, lexer.Length, "unterminated number literal");

        int length = 0;
        bool hasPeriod = false;
        for (int i = 0, max = lexer.Length; i != max; ++i)
        {
            if (char.IsWhiteSpace(lexer[i]) || Symbol.IsSymbol(lexer, i))
            {
                length = i;
                break;
            }

            if (lexer[i] is '.')
            {
                if (hasPeriod) return new Error(lexer, i, "number literal with multiple dots");
                hasPeriod = true;
            }
            else if (!char.IsNumber(lexer[i]) && lexer[i] is not '_')
            {
                return new Error(lexer, i, $"number literal with non-numeric character '{lexer[i]}' at {i}");
            }

            ++length;
        }

        //return hasPeriod ? new Literal(lexer, length, number) : null;
        return new Literal(lexer, length, number);
    }

    private static Lexeme LexTextLiteral(Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not '"') return null;

        var index = 1;
        var length = lexer[index..].Span.IndexOf('"');
        if (length is < 0) return new Error(lexer, lexer.Length, "unterminated text literal");

        while (lexer[index + length - 1] is '\\' && length < lexer.Length && length != -1)
        {
            index += length + 1;
            length = lexer[index..].Span.IndexOf('"');
        }

        if (length is < 0) return new Error(lexer, lexer.Length, "unterminated text literal");

        length += index + 1;
        for (var i = index; i != length; ++i)
        {
            if (lexer[i] is '\n') ++lexer.Line;
        }

        return new Literal(lexer, length, text);
    }

    private static Literal LexTimeLiteral(Lexer lexer)
        => LexTwoDigitWithSpacedSuffixTimeLiteral(lexer)
        ?? LexTwoDigitWithUnspacedSuffixTimeLiteral(lexer)
        ?? LexTwoDigitWithoutSuffixTimeLiteral(lexer)
        ?? LexOneDigitWithSpacedSuffixTimeLiteral(lexer)
        ?? LexOneDigitWithUnspacedSuffixTimeLiteral(lexer);

    private static Literal LexTwoDigitWithSpacedSuffixTimeLiteral(Lexer lexer)
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
        : new Literal(lexer, 10, time);

    private static Literal LexTwoDigitWithUnspacedSuffixTimeLiteral(Lexer lexer)
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
        : new Literal(lexer, 9, time);

    private static Literal LexTwoDigitWithoutSuffixTimeLiteral(Lexer lexer)
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
        : new Literal(lexer, 8, time);

    private static Literal LexOneDigitWithSpacedSuffixTimeLiteral(Lexer lexer)
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
        : new Literal(lexer, 9, time);

    private static Literal LexOneDigitWithUnspacedSuffixTimeLiteral(Lexer lexer)
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
        : new Literal(lexer, 8, time);

    private static Literal LexUrlLiteral(Lexer lexer)
    {
        if (lexer.Length is < 5) return null;

        // get scheme
        int length = 0;
        while (length < lexer.Length && char.IsLetter(lexer[length])) ++length;
        if (length == lexer.Length) return null;

        if (length + 4 >= lexer.Length || lexer[length] is not ':' || lexer[length + 1] is not '/' || lexer[length + 2] is not '/') return null;

        length += 3;
        while (length < lexer.Length && IsValidUrlCharacter(lexer[length])) ++length;

        return new Literal(lexer, length, url);
    }

    private static bool IsValidUrlCharacter(char value) => char.IsLetterOrDigit(value) || value is '~' or '*' or '(' or ')' or '.' or '-' or '_' or '/';
}
