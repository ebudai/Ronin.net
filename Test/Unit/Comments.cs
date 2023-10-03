using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait("Lexer", null)]
public class Comments
{
    public const string singleline = Comment.SingleLine.Start;
    public const string multilinestart = Comment.Multiline.Start;
    public const string multilineend = Comment.Multiline.End;

    [Fact(DisplayName = "single-line")]
    public void SingleLine()
    {
        const string comment = $"{singleline} this is a comment";
        const string extra = "\r\n\r\n more things";
        const string literal = comment + extra;

        Lexer lexer = new(literal);
        var token = Comment.Lex(ref lexer);

        Assert.Equal(literal.ToArray()[..^4], token?.Memory.ToArray());
    }

    [Fact(DisplayName = "single-line at end of file")]
    public void SingleLineEoF()
    {
        const string literal = $"{singleline} this is a comment";

        Lexer lexer = new(literal);
        var comment = Comment.Lex(ref lexer);

        Assert.Equal(literal.ToArray(), comment?.Memory.ToArray());
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

        Assert.Equal(literal[..^2].ToArray(), comment?.Memory.ToArray());
    }

    [Fact(DisplayName = "multiline nested")]
    public void NestedMultiline()
    {
        const string literal = $"{multilinestart}\n\n this{multilinestart} is a c{multilineend}omment\n\n{multilineend}";

        Lexer lexer = new(literal);
        var comment = Comment.Lex(ref lexer);

        Assert.Equal(literal.ToArray(), comment?.Memory.ToArray());
    }
}
