using Ronin.Compiler;
using Ronin.Token;

namespace Failure;

public class IntegerLiteral
{
    [Fact(DisplayName = "doesn't start with a number")]
    public void DoesntStartWithANumber()
    {
        const string literal = "g98723";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "contains invalid chars")]
    public void Invalid()
    {
        const string literal = "92v5";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Error>(lexed);
        var error = lexed as Error;
        Assert.Equal("92".ToArray(), error.Sourcecode.ToArray());
        Assert.Equal("integer literal with non-numeric character 'v' at 2", error.Message);
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "contains a dot")]
    public void Dot()
    {
        const string number = "98723.2";

        Lexer lexer = new(number);
        var lexed = Literal.Lex(lexer);

        Assert.IsType<Literal>(lexed);
        
        var literal = lexed as Literal;
        
        Assert.NotEqual(Literal.Kind.integer, literal.LiteralKind);
    }
}
