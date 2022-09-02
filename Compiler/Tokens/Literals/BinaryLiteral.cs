using Ronin.Compiler;

namespace Ronin.Tokens.Literals;

internal class BinaryLiteral : Token, ILexable<BinaryLiteral>
{
    public BinaryLiteral(Lexer lexer, int length) : base(lexer, length) { }

    public static BinaryLiteral Lex(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span;
        if (span.IsEmpty) return null;
        if (span[0] is not '0' || span[1] is not 'b' and not 'B') return null;
        
        if (span.Length is <= 2)
        {
            lexer.Error = "unterminated hex literal";
            return null;
        }
        int length = 2;
        for (int i = 2, max = span.Length; i != max; ++i)
        {
            if (span[i] is '0' or '1' or '_')
            {
                ++length;
                continue;
            }

            if (char.IsWhiteSpace(span[i]) || span[i] is '(' or ')' or '[' or ']' or '{' or '}' or ',' or '.' or '\'' or '"')
            {
                length = i;
                break;
            }

            lexer.Error = $"invalid char '{span[i]}' at {i} for binary literal";
            return null;
        }
        return new BinaryLiteral(lexer, length);
    }
}