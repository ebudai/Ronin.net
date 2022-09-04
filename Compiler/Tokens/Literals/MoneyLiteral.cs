using Ronin.Compiler;

namespace Ronin.Tokens.Literals;

internal class MoneyLiteral : Token, ILexable<MoneyLiteral>
{
    public MoneyLiteral(Lexer lexer, int length) : base(lexer, length) { }

    public static MoneyLiteral Lex(Lexer lexer)
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

        return new MoneyLiteral(lexer, length);
    }
}
