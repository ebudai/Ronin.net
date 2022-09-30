using Ronin.Compiler;

namespace Ronin.Token;

internal class Comment : Lexeme
{
    internal Comment(Lexer lexer, int length) : base(lexer, length) { }

    internal static Lexeme Lex(Lexer lexer)
    {
        if (lexer.StartsWith("//"))
        {
            var linelength = lexer.Span.IndexOfAny(Environment.NewLine.ToCharArray());
            if (linelength is < 0) linelength = lexer.Length;
            return new Comment(lexer, linelength);
        }

        if (!lexer.StartsWith("/*")) return null;

        int depth = 0;
        var length = 3;
        
        for (; length < lexer.Length; ++length)
        {
            var innerspan = lexer[length..].Span;
            if (innerspan.StartsWith("/*")) ++depth;
            else if (innerspan.StartsWith("*/")) --depth;
            if (depth is -1) break;
        }
        
        if (depth is not -1) return new Error(lexer, length, "unterminated multiline comment");

        return new Comment(lexer, length + 2);
    }
}
