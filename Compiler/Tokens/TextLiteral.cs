using Ronin.Compiler;

namespace Ronin.Tokens;

internal class TextLiteral : Token, ILexable<TextLiteral>
{
    internal TextLiteral(Lexer lexer, int length) : base(lexer, length) { }

    public static TextLiteral Lex(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span;
        if (span[0] is not '"') return null;

        var index = 1;
        var length = span[index..].IndexOf('"');
        if (length is < 0)
        {
            lexer.Error = "unterminated text literal";
            return null;
        }
        while (span[index + length - 1] is '\\')
        {
            index += length + 1;
            length = span[index..].IndexOf('"');
        }
        return new TextLiteral(lexer, index + length + 1);
    }
}
