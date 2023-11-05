using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait(nameof(Lexer), null)]
public class Literals
{
    [Fact(DisplayName = "basic date")]
    public void Date()
    {
        const string literal = "1984-05-04";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer) as Date;

        Assert.Equal(literal, lexed?.Memory.ToString());
    }

    [Fact(DisplayName = "basic text")]
    public void Text()
    {
        const string literal = "\"testtest\"";

        Lexer lexer = new(literal);
        var text = Literal.Lex(ref lexer) as Text;

        Assert.Equal(literal, text?.Memory.ToString());
    }

    [Fact(DisplayName = "with escaped quotes")]
    public void Escaped()
    {
        const string literal = @"""tes\""tte\""st""";

        Lexer lexer = new(literal);
        var text = Literal.Lex(ref lexer) as Text;

        Assert.Equal(literal, text?.Memory.ToString());
    }

    [Fact(DisplayName = "multiline")]
    public void Multiline()
    {
        const string literal = "\"test\n\nanother test\"";

        Lexer lexer = new(literal);
        var text = Literal.Lex(ref lexer) as Text;

        Assert.Equal(literal, text?.Memory.ToString());
    }

    [Fact(DisplayName = "value")]
    public void Value()
    {
        const string literal = "\"testtest\"";

        Lexer lexer = new(literal);
        var text = Literal.Lex(ref lexer) as Text;

        Assert.Equal(literal, text?.Memory.ToString());
    }
}
