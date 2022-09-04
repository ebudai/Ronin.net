using Ronin.Compiler;

namespace Ronin.Tokens.Literals;

internal class HexLiteral : Token, ILexable<HexLiteral>
{
    public HexLiteral(Lexer lexer, int length) : base(lexer, length) { }

    public static HexLiteral Lex(Lexer lexer)
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
        return new HexLiteral(lexer, length);
    }
}