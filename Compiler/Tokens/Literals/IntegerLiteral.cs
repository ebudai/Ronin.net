using Ronin.Compiler;

namespace Ronin.Tokens.Literals;

internal class IntegerLiteral : Token, ILexable<IntegerLiteral>
{
    public IntegerLiteral(Lexer lexer, int length) : base(lexer, length) { }

    public static IntegerLiteral Lex(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span[lexer.Cursor..];
        if (span.IsEmpty || !char.IsNumber(span[0])) return null;

        int length = 0;
        for (int i = 0, max = span.Length; i != max; ++i)
        {
            if (span[i] is '.') return null;

            if (char.IsWhiteSpace(span[i]) || span[i] is '(' or ')' or '[' or ']' or '{' or '}' or ',' or '\'' or '"' or ';')
            {
                length = i;
                break;
            }

            if (!char.IsNumber(span[i]) && span[i] is not '_')
            {
                lexer.Error = "integer literal with non-numeric character";
                return null;
            }

            ++length;
        }

        return new IntegerLiteral(lexer, length);
    }
}