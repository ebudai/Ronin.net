using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class CommentStart : Symbol
{
    public const string singleline = "//";
    public const string multiline = "/*";

    private CommentStart(Lexer lexer, int length) : base(lexer, length) { }

    public static new Token Lex(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;
        if (lexer.StartsWith(singleline)) return new CommentStart(lexer, singleline.Length);
        if (lexer.StartsWith(multiline)) return new CommentStart(lexer, multiline.Length);
        return null;
    }
}
