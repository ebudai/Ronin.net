using Ronin.Compiler;

namespace Ronin.Tokens;

internal class HexLiteral : Token, ILexable<HexLiteral>
{
    public HexLiteral(Lexer lexer, int length) : base(lexer, length) { }

    public static HexLiteral Lex(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span;
        if (span.IsEmpty) return null;
        if (span[0] is '0' && span[1] is 'x' or 'X')
        {
            if (span.Length is <= 2)
            {
                lexer.Error = "unterminated hex literal";
                return null;
            }
            int length = 2;
            for (int i = 2, max = span.Length; i != max; ++i)
            {
                if (char.IsNumber(span[i]) || span[i] is 'A' or 'a' or 'B' or 'b' or 'C' or 'c' or 'D' or 'd' or 'E' or 'e' or 'F' or 'f' or '_')
                {
                    ++length;
                    continue;
                }
                if (char.IsWhiteSpace(span[i]) || span[i] is '(' or ')' or '[' or ']' or '{' or '}' or ',' or '.' or '\'' or '"')
                {
                    length = i;
                    break;
                }

                lexer.Error = $"invalid char '{span[i]}' at {i} for hex literal";
                return null;
            }
            return new HexLiteral(lexer, length);
        }
        return null;
    }
}
