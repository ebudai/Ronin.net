using Ronin.Compiler;

namespace Ronin.Token.Delimiter;

internal class CommentStart : Symbol
{
    internal const string singleline = "//";
    internal const string multiline = "/*";

    public CommentStart(Lexer lexer, int length) : base(lexer, length) { }

    public static new Lexeme Lex(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;
        if (lexer.StartsWith(singleline)) return new CommentStart(lexer, singleline.Length);
        if (lexer.StartsWith(multiline)) return new CommentStart(lexer, multiline.Length);
        return null;
    }
}
