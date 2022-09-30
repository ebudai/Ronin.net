using Ronin.Compiler;

namespace Ronin.Token;

internal class Whitespace : Lexeme
{
    internal Whitespace(Lexer lexer, int length) : base(lexer, length) { }

    internal static Whitespace Lex(Lexer lexer)
    {
        var length = 0;
        while (length < lexer.Length && char.IsWhiteSpace(lexer[length]))
        {
            if (lexer[length] is '\n') ++lexer.Line;
            ++length;
        }
        if (length is 0) return null;
        return new Whitespace(lexer, length);
    }
}
