using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait("Lexer", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class comment
{
    public const string singleline = Comment.SingleLine.Start;
    public const string multilinestart = Comment.Multiline.Start;
    public const string multilineend = Comment.Multiline.End;

    [Fact(DisplayName = "single-line")]
    public void SingleLine()
    {
        const string literal = $"{singleline} this is a comment\r\n\r\n";

        Lexer lexer = new(literal);
        var comment = Comment.Lex(ref lexer);

        Assert.Equal(literal.ToArray()[..^4], comment?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "single-line at end of file")]
    public void SingleLineEoF()
    {
        const string literal = $"{singleline} this is a comment";

        Lexer lexer = new(literal);
        var comment = Comment.Lex(ref lexer);

        Assert.Equal(literal.ToArray(), comment?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "multiline")]
    public void Multiline()
    {
        const string literal = $"""
         {multilinestart}
               this is a comment
            {multilineend}

         """;

        Lexer lexer = new(literal);
        var comment = Comment.Lex(ref lexer);

        Assert.Equal(literal[..^2].ToArray(), comment?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "multiline nested")]
    public void NestedMultiline()
    {
        const string literal = $"{multilinestart}\n\n this{multilinestart} is a c{multilineend}omment\n\n{multilineend}";

        Lexer lexer = new(literal);
        var comment = Comment.Lex(ref lexer);

        Assert.Equal(literal.ToArray(), comment?.Sourcecode.ToArray());
    }
}
