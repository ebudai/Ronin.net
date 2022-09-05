using Ronin.Compiler;

namespace Ronin.Tokens.Literals;

internal class TextLiteral : Token, ILexable<TextLiteral>
{
    internal TextLiteral(Lexer lexer, int length) : base(lexer, length)
    {
        for (var i = 0; i != length; ++i)
        {
            if (Sourcecode.Span[i] is '\n') ++lexer.Line;
        }
    }

    public static TextLiteral Lex(Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not '"') return null;

        var index = 1;
        var length = lexer[index..].Span.IndexOf('"');
        if (length is < 0)
        {
            lexer.Error = "unterminated text literal";
            return null;
        }
        while (lexer[index + length - 1] is '\\' && length < lexer.Length && length != -1)
        {
            index += length + 1;
            length = lexer[index..].Span.IndexOf('"');
        }

        if (length is < 0)
        {
            lexer.Error = "unterminated text literal";
            return null;
        }
        return new TextLiteral(lexer, index + length + 1);
    }
}