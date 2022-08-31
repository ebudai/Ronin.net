using Ronin.Compiler;

namespace Ronin.Tokens;

internal class Whitespace : Token, ILexable<Whitespace>
{
    public Whitespace(Lexer lexer, int length) : base(lexer, length) { }

    public static Whitespace Lex(Lexer lexer)
    {
        if (lexer.Sourcecode.IsEmpty) return null;
        var length = 0;
        while (length < lexer.Sourcecode.Length && char.IsWhiteSpace(lexer.Sourcecode.Span[length])) ++length;
        if (length is 0) return null;
        return new Whitespace(lexer, length);
    }
}
