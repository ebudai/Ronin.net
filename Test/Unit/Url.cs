using Ronin.Compiler;
using Ronin.Lexicon;

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
        var lexed = Literal.Lex(ref lexer);

        Assert.Equal(literal.ToArray(), lexed?.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "terminated")]
    public void Terminated()
    {
        const string literal = "http://test.com;";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.Equal(literal[..^1].ToArray(), lexed?.Sourcecode.ToArray());
    }
}
