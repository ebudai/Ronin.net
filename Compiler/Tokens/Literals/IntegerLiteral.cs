using Ronin.Compiler;

namespace Ronin.Tokens.Literals;

internal class IntegerLiteral : Token, ILexable<IntegerLiteral>
{
    public IntegerLiteral(Lexer lexer, int length) : base(lexer, length) { }

    public static IntegerLiteral Lex(Lexer lexer)
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

        return new IntegerLiteral(lexer, length);
    }
}