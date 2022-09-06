using Ronin.Compiler;
using Unit;

namespace Failure;

public class Comment
{
    [Fact(DisplayName = "single-line without //")]
    public void SingleLineFail()
    {
        const string notcomment = "not a comment";

        Ronin.Compiler.Lexer lexer = new(notcomment);
        var comment = Ronin.Token.Comment.Lex(lexer);

        Assert.Null(comment);
    }

    [Fact(DisplayName = "multiline without /*")]
    public void MultiLineFail()
    {
        const string notcomment = "not a comment";

        Ronin.Compiler.Lexer lexer = new(notcomment);
        var comment = Ronin.Token.Comment.Lex(lexer);

        Assert.Null(comment);
    }

    [Fact(DisplayName = "unbalanced nested multiline")]
    public void NestedMultiLineFail()
    {
        const string badcomment = "/*not /*a comment*/";

        Ronin.Compiler.Lexer lexer = new(badcomment);
        var comment = Ronin.Token.Comment.Lex(lexer);

        Assert.NotNull(comment);
        Assert.IsType<Ronin.Token.Error>(comment);
        var error = comment as Ronin.Token.Error;
        Assert.Equal(badcomment.ToArray(), error.Sourcecode.ToArray());
        Assert.Equal("unterminated multiline comment", error.Message);
    }
}
