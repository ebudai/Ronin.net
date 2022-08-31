using Ronin.Compiler;
using Ronin.Tokens;

namespace Failure;

public class Comment
{
    [Fact(DisplayName = "single-line without //")]
    public void SingleLineFail()
    {
        const string notcomment = "not a comment";

        Lexer lexer = new() { Sourcecode = notcomment.AsMemory() };
        var comment = Ronin.Tokens.Comment.Lex(lexer);

        Assert.Null(comment);
    }

    [Fact(DisplayName = "multiline without /*")]
    public void MultiLineFail()
    {
        const string notcomment = "not a comment";

        Lexer lexer = new() { Sourcecode = notcomment.AsMemory() };
        var comment = Ronin.Tokens.Comment.Lex(lexer);

        Assert.Null(comment);
    }

    [Fact(DisplayName = "unbalanced nested multiline")]
    public void NestedMultiLineFail()
    {
        const string badcomment = "/*not /*a comment*/";

        Lexer lexer = new() { Sourcecode = badcomment.AsMemory() };
        var comment = Ronin.Tokens.Comment.Lex(lexer);

        Assert.Null(comment);
        Assert.NotNull(lexer.Error);
        Assert.NotEmpty(lexer.Error);
    }
}
