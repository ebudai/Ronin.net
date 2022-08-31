using Ronin.Compiler;

namespace Ronin.Tokens;

internal class NumberLiteral : Token, ILexable<NumberLiteral>
{
    public NumberLiteral(Lexer lexer, int length) : base(lexer, length) { }

    public static NumberLiteral Lex(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span;

        if (!char.IsNumber(span[0]))
        {
            return null;
        }

        if (span.Length is <= 3)
        {
            lexer.Error = "unterminated number literal";
            return null;
        }

        int length = 0;
        bool hasPeriod = false;
        for (int i = 0, max = span.Length; i != max; ++i)
        {
            if (char.IsWhiteSpace(span[i]) || span[i] is '(' or ')' or '[' or ']' or '{' or '}' or ',' or '\'' or '"')
            {
                length = i;
                break;
            }

            if (!char.IsNumber(span[i]) && span[i] is not '_' and not '.')
            {
                lexer.Error = "number literal with non-numeric character";
                return null;
            }

            if (span[i] is '.')
            {
                if (hasPeriod)
                {
                    length = i;
                    break;
                }
                hasPeriod = true;
            }

            ++length;
        }

        return hasPeriod ? new NumberLiteral(lexer, length) : null;
    }
}
