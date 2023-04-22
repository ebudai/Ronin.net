using Ronin;
using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait("Lexer", null)]
public class Url
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string literal = "http://test.com";

        Lexer lexer = new(literal);
        var url = Literal.Lex(ref lexer) as UrlLiteral;

        Assert.Equal(literal, url?.ToString());
    }

    [Fact(DisplayName = "terminated")]
    public void Terminated()
    {
        const string literal = "http://test.com;";

        Lexer lexer = new(literal);
        var url = Literal.Lex(ref lexer) as UrlLiteral;

        Assert.Equal(literal[..^1], url?.ToString());
    }
}
