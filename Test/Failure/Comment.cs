using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

[Trait("Lexer", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class comment
{
    [Fact(DisplayName = "no comment start")]
    public void Basic()
    {
        const string notcomment = "not a comment";

        Lexer lexer = new(notcomment);
        var comment = Comment.Lex(lexer);

        Assert.Null(comment);
    }

    [Fact(DisplayName = "unbalanced nested multiline start")]
    public void UnbalancedMultiLineStart()
    {
        const string badcomment = "/*unbalanced /*comment*/\r\nthis is a function call();";

        Lexer lexer = new(badcomment);
        var comment = Comment.Lex(lexer);

        Assert.False(comment?.Terminated);
        Assert.Equal(badcomment, comment?.ToString());
    }

    [Fact(DisplayName = "unbalanced nested multiline end")]
    public void UnbalancedMultiLineEnd()
    {
        const string badcomment = "/*unbalanced */comment*/";

        Lexer lexer = new(badcomment);
        var comment = Comment.Lex(lexer);

        Assert.True(comment?.Terminated);
        Assert.Equal("/*unbalanced */", comment?.ToString());
    }
}
