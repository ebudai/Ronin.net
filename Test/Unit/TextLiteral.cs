using Ronin.Lexicon;
using Ronin.Lexicon.Literals;

namespace Unit;

[Trait("Lexer", null)]
public class TextLiteral
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string literal = "\"testtest\"";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "with escaped quotes")]
    public void Escaped()
    {
        const string literal = @"""tes\""tte\""st""";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "multiline")]
    public void Multiline()
    {
        const string literal = "\"test\n\nanother test\"";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(3, lexer.Line);
    }

    [Fact(DisplayName = "value")]
    public void Value()
    {
        const string literal = "\"testtest\"";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        var text = lexed as Text;
        Assert.NotNull(text);
        Assert.Equal("testtest", text.Value);
    }
}