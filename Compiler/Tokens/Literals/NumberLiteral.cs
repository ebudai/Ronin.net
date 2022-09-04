using Ronin.Compiler;

namespace Ronin.Tokens.Literals;

internal class NumberLiteral : Token, ILexable<NumberLiteral>
{
    public NumberLiteral(Lexer lexer, int length) : base(lexer, length) { }

    public static NumberLiteral Lex(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;

        if (!char.IsNumber(lexer[0]))
        {
            return null;
        }

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

        return hasPeriod ? new NumberLiteral(lexer, length) : null;
    }
}