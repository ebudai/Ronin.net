using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;

namespace Unit;

[Trait("Lexer", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class text
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string literal = "\"testtest\"";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Equal(literal.ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with escaped quotes")]
    public void Escaped()
    {
        const string literal = @"""tes\""tte\""st""";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Equal(literal.ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "multiline")]
    public void Multiline()
    {
        const string literal = "\"test\n\nanother test\"";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Equal(literal.ToArray(), lexed?.Sourcecode.ToArray());
        Assert.Equal(3, lexer.Line);
    }

    [Fact(DisplayName = "value")]
    public void Value()
    {
        const string literal = "\"testtest\"";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        var text = lexed as Text;
        Assert.Equal("testtest", text?.Value);
    }
}