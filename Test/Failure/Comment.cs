using Ronin.Compiler;

namespace Failure;

[Trait("Lexer", null)]
public class Comment
{
    public const string singleline = Ronin.Lexicon.Comment.SingleLine.Start;
    public const string multilinestart = Ronin.Lexicon.Comment.Multiline.Start;
    public const string multilineend = Ronin.Lexicon.Comment.Multiline.End;
        
    [Fact(DisplayName = "no comment start")]
    public void Basic()
    {
        const string notcomment = "not a comment";

        Lexer lexer = new(notcomment);
        var comment = Ronin.Lexicon.Comment.Lex(ref lexer);

        Assert.Null(comment);
    }

    [Fact(DisplayName = "unbalanced nested multiline start")]
    public void UnbalancedMultiLineStart()
    {
        const string badcomment = $"{multilinestart}unbalanced {multilinestart}comment{multilineend}\r\nthis is a function call();";

        Lexer lexer = new(badcomment);
        var comment = Ronin.Lexicon.Comment.Lex(ref lexer);

        Assert.False(comment?.Terminated);
        Assert.Equal(badcomment, comment?.sourcecode.ToString());
    }

    [Fact(DisplayName = "unbalanced nested multiline end")]
    public void UnbalancedMultiLineEnd()
    {
        const string badcomment = $"{multilinestart}unbalanced {multilineend}comment{multilineend}";

        Lexer lexer = new(badcomment);
        var comment = Ronin.Lexicon.Comment.Lex(ref lexer);

        Assert.True(comment?.Terminated);
        Assert.Equal("/*unbalanced */", comment?.sourcecode.ToString());
    }
}
