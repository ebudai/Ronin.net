using Ronin.Compiler;
using Ronin.Tokens;

namespace Unit;

public class CommentUnitTests
{
    [Fact(DisplayName = "parse single-line comment")]
    public void SingleLine()
    {
        const string literal = "// this is a comment\n\n";

        Lexer lexer = new() { Sourcecode = literal.AsMemory() };
        var comment = Comment.Lex(lexer);

        Assert.NotNull(comment);
        Assert.Equal(literal.Trim().ToArray(), comment.Sourcecode.ToArray());
        Assert.Equal(literal.Trim().Length, comment.SourceIndex);
    }

    [Fact(DisplayName = "parse single-line comment at end of file")]
    public void SingleLineEoF()
    {
        const string literal = "// this is a comment";

        Lexer lexer = new() { Sourcecode = literal.AsMemory() };
        var comment = Comment.Lex(lexer);

        Assert.NotNull(comment);
        Assert.Equal(literal.Trim().ToArray(), comment.Sourcecode.ToArray());
        Assert.Equal(literal.Trim().Length, comment.SourceIndex);
    }

    [Fact(DisplayName = "parse multiline comment")]
    public void Multiline()
    {
        const string literal = "/*\n\n this is a comment\n\n*/";

        Lexer lexer = new() { Sourcecode = literal.AsMemory() };
        var comment = Comment.Lex(lexer);

        Assert.NotNull(comment);
        Assert.Equal(literal.ToArray(), comment.Sourcecode.ToArray());
        Assert.Equal(literal.Length, comment.SourceIndex);
    }

    [Fact(DisplayName = "parse multiline comment")]
    public void NestedMultiline()
    {
        const string literal = "/*\n\n this/* is a c*/omment\n\n*/";

        Lexer lexer = new() { Sourcecode = literal.AsMemory() };
        var comment = Comment.Lex(lexer);

        Assert.NotNull(comment);
        Assert.Equal(literal.ToArray(), comment.Sourcecode.ToArray());
        Assert.Equal(literal.Length, comment.SourceIndex);
    }
}
