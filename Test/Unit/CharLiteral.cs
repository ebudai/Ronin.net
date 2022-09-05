using Ronin.Compiler;
using Ronin.Token;

namespace Unit;

public class CharLiteral
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string literal = "'c'";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "unicode")]
    public void Unicode()
    {
        const string literal = "'\u44A2'";

        Ronin.Compiler.Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
    }
}
