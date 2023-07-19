using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

[Trait("Lexer", null)]
public class Comments
{
    public const string singleline = Comment.SingleLine.Start;
    public const string multilinestart = Comment.Multiline.Start;
    public const string multilineend = Comment.Multiline.End;
        
    [Fact(DisplayName = "no comment start")]
    public void Basic()
    {
        const string notcomment = "not a comment";

        Lexer lexer = new(notcomment);
        var comment = Comment.Lex(ref lexer);

        Assert.Null(comment);
    }

    [Fact(DisplayName = "unbalanced nested multiline start")]
    public void UnbalancedMultiLineStart()
    {
        const string badcomment = $"{multilinestart}unbalanced {multilinestart}comment{multilineend}\r\nthis is a function call();";

        Lexer lexer = new(badcomment);
        var comment = Comment.Lex(ref lexer);

        Assert.False(comment?.Terminated);
        Assert.Equal(badcomment, comment?.Memory.ToString());
    }

    [Fact(DisplayName = "unbalanced nested multiline end")]
    public void UnbalancedMultiLineEnd()
    {
        const string badcomment = $"{multilinestart}unbalanced {multilineend}comment{multilineend}";

        Lexer lexer = new(badcomment);
        var comment = Comment.Lex(ref lexer);

        Assert.True(comment?.Terminated);
        Assert.Equal("/*unbalanced */", comment?.Memory.ToString());
    }
}
