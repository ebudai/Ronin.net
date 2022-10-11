using Ronin.Compiler;

namespace Failure;

public class Comment
{
    [Fact(DisplayName = "no comment start")]
    public void Basic()
    {
        const string notcomment = "not a comment";

        Lexer lexer = new(notcomment);
        var comment = Ronin.Token.Comment.Lex(lexer);

        Assert.Null(comment);
    }

    [Fact(DisplayName = "unbalanced nested multiline start")]
    public void UnbalancedMultiLineStart()
    {
        const string badcomment = "/*unbalanced /*comment*/\r\nthis is a function call();";

        Lexer lexer = new(badcomment);
        var lexeme = Ronin.Token.Comment.Lex(lexer);

        Assert.NotNull(lexeme);
        Assert.IsType<Ronin.Token.Comment>(lexeme);
        var comment = lexeme as Ronin.Token.Comment;
        Assert.False(comment.Terminated);
        Assert.Equal(badcomment, comment.ToString());
    }

    [Fact(DisplayName = "unbalanced nested multiline end")]
    public void UnbalancedMultiLineEnd()
    {
        const string badcomment = "/*unbalanced */comment*/";

        Lexer lexer = new(badcomment);
        var lexeme = Ronin.Token.Comment.Lex(lexer);

        Assert.NotNull(lexeme);
        Assert.IsType<Ronin.Token.Comment>(lexeme);
        var comment = lexeme as Ronin.Token.Comment;
        Assert.True(comment.Terminated);
        Assert.Equal("/*unbalanced */", comment.ToString());
    }
}
