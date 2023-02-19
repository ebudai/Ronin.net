using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;

namespace Unit;

[Trait("Lexer", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class url
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string literal = "http://test.com";

        Lexer lexer = new(literal);
        var url = Literal.Lex(ref lexer) as Url;

        Assert.Equal(literal, url?.ToString());
    }

    [Fact(DisplayName = "terminated")]
    public void Terminated()
    {
        const string literal = "http://test.com;";

        Lexer lexer = new(literal);
        var url = Literal.Lex(ref lexer) as Url;

        Assert.Equal(literal[..^1], url?.ToString());
    }
}
