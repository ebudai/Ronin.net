using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait(nameof(Lexer), null)]
public class Comments
{
    public const string singleline = Comment.SingleLine.Start;
    public const string multilinestart = Comment.Multiline.Start;
    public const string multilineend = Comment.Multiline.End;

    [Fact(DisplayName = "single-line comment stops at end of a line")]
    public void SingleLine()
    {
        const string comment = $"{singleline} this is a comment";
        const string extra = "\r\n\r\n more things";
        const string text = comment + extra;

        Lexer lexer = new(text);
        var token = Comment.Lex(ref lexer);

        Assert.Equal(comment.ToArray(), token?.Memory.ToArray());
    }

    [Fact(DisplayName = "single-line stops at end of file")]
    public void SingleLineEoF()
    {
        const string text = $"{singleline} this is a comment";

        Lexer lexer = new(text);
        var comment = Comment.Lex(ref lexer);

        Assert.Equal(text.ToArray(), comment?.Memory.ToArray());
    }

    [Fact(DisplayName = "multiline comment stops at end of file")]
    public void Multiline()
    {
        const string literal = $"""
         {multilinestart}
               this is a comment
            {multilineend}

         """;

        Lexer lexer = new(literal);
        var comment = Comment.Lex(ref lexer);

        // the comment ends immediately after the terminator and excludes the
        // trailing newline. Sliced by index rather than by a fixed offset from the
        // end, because .gitattributes normalises line endings and the newline this
        // literal ends with is one character here and two on Windows.
        var end = literal.IndexOf(multilineend) + multilineend.Length;
        Assert.Equal(literal[..end].ToArray(), comment?.Memory.ToArray());
    }

    [Fact(DisplayName = "multiline comments can be nested")]
    public void NestedMultiline()
    {
        const string literal = $"{multilinestart}\n\n this{multilinestart} is a c{multilineend}omment\n\n{multilineend}";

        Lexer lexer = new(literal);
        var comment = Comment.Lex(ref lexer);

        Assert.Equal(literal.ToArray(), comment?.Memory.ToArray());
    }
}
