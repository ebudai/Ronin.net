using Ronin.Compiler;

namespace Ronin.Tokens;

internal class Comment : Token, ILexable<Comment>
{
    internal Comment(Lexer lexer, int length) : base(lexer, length) { }

    public static Comment Lex(Lexer lexer)
    {
        if (lexer.Sourcecode.Span.StartsWith("//"))
        {
            var length = lexer.Sourcecode.Span.IndexOf('\n');
            if (length is < 0) length = lexer.Sourcecode.Length;
            return new Comment(lexer, length);
        }
        else if (lexer.Sourcecode.Span.StartsWith("/*"))
        {
            int depth = 0;
            var length = 3;
            for (; length < lexer.Sourcecode.Length; ++length)
            {
                var span = lexer.Sourcecode[length..].Span;
                if (span.StartsWith("/*")) ++depth;
                else if (span.StartsWith("*/")) --depth;
                if (depth is -1) break;
            }
            if (depth is not -1)
            {
                lexer.Error = "unterminated multiline comment";
                return null;
            }
            return new Comment(lexer, length + 2);
        }
        return null;        
    }
}
