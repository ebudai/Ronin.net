using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

[Trait("Lexer", null)]
public class BinaryLiteral
{
    [Fact(DisplayName = "doesn't start with 0x")]
    public void Fail()
    {
        const string literal = "not a binary literal";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }

    [Fact(DisplayName = "unterminated")]
    public void Unterminated()
    {
        const string literal = "0b";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Ronin.Lexicon.Literals.Integer>(lexed);
        var name = lexed as Ronin.Lexicon.Literals.Integer;
        Assert.Equal(literal[..1].ToArray(), name.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "contains invalid char")]
    public void Invalid()
    {
        const string literal = "0b101023";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.IsType<Ronin.Lexicon.Literals.Binary>(lexed);
        var binary = lexed as Ronin.Lexicon.Literals.Binary;
        Assert.Equal(literal[..^2].ToArray(), binary.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }
}
