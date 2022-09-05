using Ronin.Compiler;

namespace Failure;

public class Comment
{
    [Fact(DisplayName = "single-line without //")]
    public void SingleLineFail()
    {
        const string notcomment = "not a comment";

        Lexer lexer = new(notcomment);
        var comment = Ronin.Token.Comment.Lex(lexer);

        Assert.Null(comment);
    }

    [Fact(DisplayName = "multiline without /*")]
    public void MultiLineFail()
    {
        const string notcomment = "not a comment";

        Lexer lexer = new(notcomment);
        var comment = Ronin.Token.Comment.Lex(lexer);

        Assert.Null(comment);
    }

    [Fact(DisplayName = "unbalanced nested multiline")]
    public void NestedMultiLineFail()
    {
        const string badcomment = "/*not /*a comment*/";

        Lexer lexer = new(badcomment);
        var comment = Ronin.Token.Comment.Lex(lexer);

        Assert.Null(comment);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }
}
