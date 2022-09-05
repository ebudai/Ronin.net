using Ronin.Compiler;

using static Ronin.Token.Literal.Kind;

namespace Ronin.Token;

internal class Literal : Token
{
    internal Literal(Lexer lexer, int length, Kind kind) : base(lexer, length) => LiteralKind = kind;

    internal Kind LiteralKind { get; }

    public static Token Lex(Lexer lexer)
        => LexBinaryLiteral(lexer)
        ?? LexCharacterLiteral(lexer)
        ?? LexDateLiteral(lexer)
        ?? LexHexLiteral(lexer)
        ?? LexIntegerLiteral(lexer)
        ?? LexMoneyLiteral(lexer)
        ?? LexNumberLiteral(lexer)
        ?? LexTextLiteral(lexer)
        ?? LexTimeLiteral(lexer)
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

    private static Literal LexBinaryLiteral(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;
        if (lexer[0] is not '0' || lexer[1] is not 'b' and not 'B') return null;

        if (lexer.Length is <= 2)
        {
            lexer.Error = "unterminated hex literal"; //TODO make this an error token
            return null;
        }
        int length = 2;
        for (int i = 2, max = lexer.Length; i != max; ++i)
        {
            if (lexer[i] is '0' or '1' or '_')
            {
                ++length;
                continue;
            }

            if (char.IsWhiteSpace(lexer[i]) || Symbol.IsSymbol(lexer, i) || lexer[i] is '.' or '\'' or '"')
            {
                length = i;
                break;
            }

            lexer.Error = $"invalid char '{lexer[i]}' at {i} for binary literal";
            return null;
        }
        return new Literal(lexer, length, binary);
    }

    private static Literal LexCharacterLiteral(Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not '\'') return null;

        var length = lexer[1..].Span.IndexOf('\'');
        if (length is < 0)
        {
            lexer.Error = "unterminated character literal";
            return null;
        }
        if (length is 0)
        {
            lexer.Error = "empty character literal";
            return null;
        }
        if (length is not 1 and not 6)
        {
            lexer.Error = "bad unicode literal";
            return null;
        }

        //TODO ensure all are 0-9 or abcdef or ABCDEF for unichar

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

    private static Literal LexHexLiteral(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;
        if (lexer[0] is not '0' || lexer[1] is not 'x' and not 'X') return null;

        if (lexer.Length is <= 2)
        {
            lexer.Error = "unterminated hex literal";
            return null;
        }
        int length = 2;
        for (int i = 2, max = lexer.Length; i != max; ++i)
        {
            if (char.IsWhiteSpace(lexer[i]) || lexer[i] is '(' or ')' or '[' or ']' or '{' or '}' or ',' or ';' or '\'' or '"')
            {
                length = i;
                break;
            }

            if (!char.IsNumber(lexer[i]) && lexer[i] is not 'A' and not 'a' and not 'B' and not 'b' and not 'C' and not 'c' and not 'D' and not 'd' and not 'E' and not 'e' and not 'F' and not 'f' and not '_')
            {
                lexer.Error = $"invalid char '{lexer[i]}' at {i} for hex literal";
                return null;
            }

            ++length;
        }
        return new Literal(lexer, length, hex);
    }

    private static Literal LexIntegerLiteral(Lexer lexer)
    {
        if (lexer.IsEmpty || !char.IsNumber(lexer[0])) return null;

        int length = 0;
        for (int i = 0, max = lexer.Length; i != max; ++i)
        {
            if (lexer[i] is '.') return null;

            if (char.IsWhiteSpace(lexer[i]) || lexer[i] is '(' or ')' or '[' or ']' or '{' or '}' or ',' or '\'' or '"' or ';')
            {
                length = i;
                break;
            }

            if (!char.IsNumber(lexer[i]) && lexer[i] is not '_')
            {
                lexer.Error = "integer literal with non-numeric character";
                return null;
            }

            ++length;
        }

        return new Literal(lexer, length, integer);
    }

    private static Literal LexMoneyLiteral(Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not '$') return null;

        if (lexer.Length is < 2)
        {
            lexer.Error = "unterminated money literal";
            return null;
        }

        if (!char.IsNumber(lexer[1])) return null;

        int length = 2;
        bool hasPeriod = false;
        for (int i = 2, max = lexer.Length; i != max; ++i)
        {
            if (char.IsWhiteSpace(lexer[i]) || lexer[i] is '(' or ')' or '[' or ']' or '{' or '}' or ',' or '\'' or '"' or ';')
            {
                length = i;
                break;
            }

            if (!char.IsNumber(lexer[i]) && lexer[i] is not '_' and not '.')
            {
                lexer.Error = "money literal with non-numeric character";
                return null;
            }

            if (lexer[i] is '.')
            {
                if (hasPeriod)
                {
                    lexer.Error = "money literal with multiple dots";
                    return null;
                }
                hasPeriod = true;
            }

            ++length;
        }

        if (lexer[length - 1] is '.')
        {
            lexer.Error = "money literal cannot end with a dot";
            return null;
        }

        return new Literal(lexer, length, money);
    }

    private static Literal LexNumberLiteral(Lexer lexer)
    {
        if (lexer.IsEmpty || !char.IsNumber(lexer[0])) return null;

        if (lexer.Length is < 3)
        {
            lexer.Error = "unterminated number literal";
            return null;
        }

        int length = 0;
        bool hasPeriod = false;
        for (int i = 0, max = lexer.Length; i != max; ++i)
        {
            if (char.IsWhiteSpace(lexer[i]) || lexer[i] is '(' or ')' or '[' or ']' or '{' or '}' or ',' or '\'' or '"' or ';')
            {
                length = i;
                break;
            }

            if (lexer[i] is '.')
            {
                if (hasPeriod)
                {
                    lexer.Error = "number literal with multiple periods";
                    return null;
                }
                hasPeriod = true;
            }
            else if (!char.IsNumber(lexer[i]) && lexer[i] is not '_' and not ';')
            {
                lexer.Error = "number literal with non-numeric character";
                return null;
            }

            ++length;
        }

        return hasPeriod ? new Literal(lexer, length, number) : null;
    }

    private static Literal LexTextLiteral(Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not '"') return null;

        var index = 1;
        var length = lexer[index..].Span.IndexOf('"');
        if (length is < 0)
        {
            lexer.Error = "unterminated text literal";
            return null;
        }
        while (lexer[index + length - 1] is '\\' && length < lexer.Length && length != -1)
        {
            index += length + 1;
            length = lexer[index..].Span.IndexOf('"');
        }

        if (length is < 0)
        {
            lexer.Error = "unterminated text literal";
            return null;
        }

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

        int length = 0;
        while (length < lexer.Length && char.IsLetter(lexer[length])) ++length;
        if (length == lexer.Length)
        {
            lexer.Error = "unterminated url literal";
            return null;
        }

        if (length + 4 >= lexer.Length || lexer[length] is not ':' || lexer[length + 1] is not '/' || lexer[length + 2] is not '/') return null;

        length += 3;
        while (length < lexer.Length && IsValidUrlCharacter(lexer[length])) ++length;

        return new Literal(lexer, length, url);
    }

    private static bool IsValidUrlCharacter(char value) => char.IsLetterOrDigit(value) || value is '~' or '*' or '(' or ')' or '.' or '-' or '_';
}
