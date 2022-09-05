using Ronin.Compiler;

namespace Ronin.Tokens;

internal class Comment : Token, ILexable<Comment>
{
    internal Comment(Lexer lexer, int length) : base(lexer, length) { }

    public static Comment Lex(Lexer lexer)
    {
        if (lexer.StartsWith("//"))
        {
            var linelength = lexer.IndexOfAny(Environment.NewLine.ToCharArray());
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
        if (depth is not -1)
        {
            lexer.Error = "unterminated multiline comment";
            return null;
        }
        return new Comment(lexer, length + 2);
    }
}
