using Ronin.Compiler;

namespace Ronin.Tokens;

internal class CharLiteral : Token, ILexable<CharLiteral>
{
    public CharLiteral(Lexer lexer, int length) : base(lexer, length) { }

    public static CharLiteral Lex(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span;
        if (span[0] is not '\'') return null;

        var length = span[1..].IndexOf('\'');
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
        if (length is not 1)
        {
            lexer.Error = "bad unicode literal";
            return null;
        }
        return new CharLiteral(lexer, length + 2);
    }
}
