using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class CommentEnd : Symbol
{
    public const string multiline = "*/";

    private CommentEnd(Lexer lexer) : base(lexer, multiline.Length) { }

    public static new Token Lex(Lexer lexer) => lexer.IsNotEmpty && lexer.StartsWith(multiline) ? new CommentEnd(lexer) : null;
}
