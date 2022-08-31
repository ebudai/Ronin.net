using Ronin.Compiler;
using Ronin.Tokens;

namespace Failure;

public class CommentFailureTests
{
    [Fact(DisplayName = "lex single-line comment without //")]
    public void SingleLineFail()
    {
        const string notcomment = "not a comment";

        Lexer lexer = new() { Sourcecode = notcomment.AsMemory() };
        var comment = Comment.Lex(lexer);

        Assert.Null(comment);
    }

    [Fact(DisplayName = "lex multiline comment without /*")]
    public void MultiLineFail()
    {
        const string notcomment = "not a comment";

        Lexer lexer = new() { Sourcecode = notcomment.AsMemory() };
        var comment = Comment.Lex(lexer);

        Assert.Null(comment);
    }

    [Fact(DisplayName = "lex unbalanced nested multiline comment")]
    public void NestedMultiLineFail()
    {
        const string badcomment = "/*not /*a comment*/";

        Lexer lexer = new() { Sourcecode = badcomment.AsMemory() };
        var comment = Comment.Lex(lexer);

        Assert.Null(comment);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }
}
