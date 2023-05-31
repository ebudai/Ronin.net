using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;

namespace Unit;

[Trait("Lexer", null)]
public class Literals
{
    [Fact(DisplayName = "basic date")]
    public void Date()
    {
        const string literal = "1984-05-04";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer) as Date;

        Assert.Equal(literal.ToArray(), lexed?.sourcecode.ToArray());
    }

    [Fact(DisplayName = "basic char")]
    public void Char()
    {
        const string literal = "'c'";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer) as Character;

        Assert.Equal(literal, lexed?.sourcecode.ToArray());
    }

    [Fact(DisplayName = "unicode")]
    public void Unicode()
    {
        const string literal = @"'\u44A2'";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer) as Character;

        Assert.Equal(literal, lexed?.sourcecode.ToArray());
    }

    [Fact(DisplayName = "basic text")]
    public void Text()
    {
        const string literal = "\"testtest\"";

        Lexer lexer = new(literal);
        var text = Literal.Lex(ref lexer) as Text;

        Assert.Equal(literal, text?.ToString());
    }

    [Fact(DisplayName = "with escaped quotes")]
    public void Escaped()
    {
        const string literal = @"""tes\""tte\""st""";

        Lexer lexer = new(literal);
        var text = Literal.Lex(ref lexer) as Text;

        Assert.Equal(literal, text?.ToString());
    }

    [Fact(DisplayName = "multiline")]
    public void Multiline()
    {
        const string literal = "\"test\n\nanother test\"";

        Lexer lexer = new(literal);
        var text = Literal.Lex(ref lexer) as Text;

        Assert.Equal(literal, text?.ToString());
    }

    [Fact(DisplayName = "value")]
    public void Value()
    {
        const string literal = "\"testtest\"";

        Lexer lexer = new(literal);
        var text = Literal.Lex(ref lexer) as Text;

        Assert.Equal(literal, text?.ToString());
    }

    [Fact(DisplayName = "basic url")]
    public void URL()
    {
        const string literal = "http://test.com";

        Lexer lexer = new(literal);
        var url = Literal.Lex(ref lexer) as Url;

        Assert.Equal(literal, url?.ToString());
    }

    [Fact(DisplayName = "terminated url")]
    public void TerminatedURL()
    {
        const string literal = "http://test.com;";

        Lexer lexer = new(literal);
        var url = Literal.Lex(ref lexer) as Url;

        Assert.Equal(literal[..^1], url?.ToString());
    }
}
