using Ronin.Compiler;

namespace Ronin.Token.Delimiter;

internal class CommentStart : Symbol
{
    private CommentStart(Lexer lexer, int length) : base(lexer, length) { }

    public static new Lexeme Lex(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;
        if (lexer.StartsWith(Comment.singleline)) return new CommentStart(lexer, Comment.singleline.Length);
        if (lexer.StartsWith(Comment.multilinestart)) return new CommentStart(lexer, Comment.multilinestart.Length);
        return null;
    }
}
