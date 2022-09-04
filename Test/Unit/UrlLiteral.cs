using Ronin.Compiler;

using Literal = Ronin.Tokens.Literals.UrlLiteral;

namespace Unit;

public class UrlLiteral
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string literal = "http://test.com";

        Ronin.Compiler.Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "terminated")]
    public void Terminated()
    {
        const string literal = "http://test.com;";

        Ronin.Compiler.Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal[..^1].ToArray(), lexed.Sourcecode.ToArray());
    }
}
