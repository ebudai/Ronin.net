namespace Unit;

[Trait("Lexer", null)]
public class Comment
{
    public const string singleline = Ronin.Lexicon.Comment.SingleLine.Start;
    public const string multilinestart = Ronin.Lexicon.Comment.Multiline.Start;
    public const string multilineend = Ronin.Lexicon.Comment.Multiline.End;

    [Fact(DisplayName = "single-line")]
    public void SingleLine()
    {
        const string literal = $"{singleline} this is a comment\r\n\r\n";

        Ronin.Compiler.Lexer lexer = new(literal);
        var comment = Ronin.Lexicon.Comment.Lex(lexer);

        Assert.NotNull(comment);
        Assert.Equal(literal.ToArray()[..^4], comment.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "single-line at end of file")]
    public void SingleLineEoF()
    {
        const string literal = $"{singleline} this is a comment";

        Ronin.Compiler.Lexer lexer = new(literal);
        var comment = Ronin.Lexicon.Comment.Lex(lexer);

        Assert.NotNull(comment);
        Assert.Equal(literal.ToArray(), comment.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "multiline")]
    public void Multiline()
    {
        const string literal = $"""
         {multilinestart}
               this is a comment
            {multilineend}

         """;

        Ronin.Compiler.Lexer lexer = new(literal);
        var comment = Ronin.Lexicon.Comment.Lex(lexer);

        Assert.NotNull(comment);
        Assert.Equal(literal[..^2].ToArray(), comment.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "multiline nested")]
    public void NestedMultiline()
    {
        const string literal = $"{multilinestart}\n\n this{multilinestart} is a c{multilineend}omment\n\n{multilineend}";

        Ronin.Compiler.Lexer lexer = new(literal);
        var comment = Ronin.Lexicon.Comment.Lex(lexer);

        Assert.NotNull(comment);
        Assert.Equal(literal.ToArray(), comment.Sourcecode.ToArray());
    }
}
