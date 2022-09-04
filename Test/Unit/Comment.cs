namespace Unit;

public class Comment
{
    [Fact(DisplayName = "single-line")]
    public void SingleLine()
    {
        const string literal = "// this is a comment\n\n";

        Ronin.Compiler.Lexer lexer = new(literal);
        var comment = Ronin.Tokens.Comment.Lex(lexer);

        Assert.NotNull(comment);
        Assert.Equal(literal.Trim().ToArray(), comment.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "single-line at end of file")]
    public void SingleLineEoF()
    {
        const string literal = "// this is a comment";

        Ronin.Compiler.Lexer lexer = new(literal);
        var comment = Ronin.Tokens.Comment.Lex(lexer);

        Assert.NotNull(comment);
        Assert.Equal(literal.Trim().ToArray(), comment.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "multiline")]
    public void Multiline()
    {
        const string literal = "/*\n\n this is a comment\n\n*/";

        Ronin.Compiler.Lexer lexer = new(literal);
        var comment = Ronin.Tokens.Comment.Lex(lexer);

        Assert.NotNull(comment);
        Assert.Equal(literal.ToArray(), comment.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "multiline nested")]
    public void NestedMultiline()
    {
        const string literal = "/*\n\n this/* is a c*/omment\n\n*/";

        Ronin.Compiler.Lexer lexer = new(literal);
        var comment = Ronin.Tokens.Comment.Lex(lexer);

        Assert.NotNull(comment);
        Assert.Equal(literal.ToArray(), comment.Sourcecode.ToArray());
    }
}
