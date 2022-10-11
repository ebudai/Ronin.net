using Ronin.Compiler;

namespace Ronin.Token.Value;

internal class Text : Literal
{
    internal Text(Lexer lexer, int length) : base(lexer, length) { }

    internal static new Lexeme Lex(Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not '"') return null;

        var index = 1;
        var length = lexer[index..].Span.IndexOf('"');
        if (length is < 0) return new Error(lexer, lexer.Length, "unterminated text literal");

        while (lexer[index + length - 1] is '\\' && length < lexer.Length && length != -1)
        {
            index += length + 1;
            length = lexer[index..].Span.IndexOf('"');
        }

        if (length is < 0) return new Error(lexer, lexer.Length, "unterminated text literal");

        length += index + 1;
        for (var i = index; i != length; ++i)
        {
            if (lexer[i] is '\n') ++lexer.Line;
        }

        return new Text(lexer, length);
    }
}
