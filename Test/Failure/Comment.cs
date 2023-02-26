using Ronin.Compiler;

namespace Failure;

[Trait("Lexer", null)]
public class Comment
{
    [Fact(DisplayName = "no comment start")]
    public void Basic()
    {
        const string notcomment = "not a comment";

        Lexer lexer = new(notcomment);
        var comment = Ronin.Lexicon.Comment.Lex(ref lexer);

        Assert.Null(comment);
    }

    [Fact(DisplayName = "unbalanced nested multiline start")]
    public void UnbalancedMultiLineStart()
    {
        const string badcomment = "/*unbalanced /*comment*/\r\nthis is a function call();";

        Lexer lexer = new(badcomment);
        var comment = Ronin.Lexicon.Comment.Lex(ref lexer);

        Assert.False(comment?.Terminated);
        Assert.Equal(badcomment, comment?.sourcecode.ToString());
    }

    [Fact(DisplayName = "unbalanced nested multiline end")]
    public void UnbalancedMultiLineEnd()
    {
        const string badcomment = "/*unbalanced */comment*/";

        Lexer lexer = new(badcomment);
        var comment = Ronin.Lexicon.Comment.Lex(ref lexer);

        Assert.True(comment?.Terminated);
        Assert.Equal("/*unbalanced */", comment?.sourcecode.ToString());
    }
}
