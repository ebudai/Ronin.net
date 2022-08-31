using Ronin.Compiler;
using Ronin.Tokens;

namespace Unit;

public class CharLiteralUnitTests
{
    [Fact(DisplayName = "parse basic char literal")]
    public void Basic()
    {
        const string literal = "'c'";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = CharLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length, lexed.SourceIndex);
    }

    [Fact(DisplayName = "parse unicode char literal")]
    public void Unicode()
    {
        const string literal = "'\u44A2'";

        Lexer lexer = new() { Sourcecode = literal.ToArray() };
        var lexed = CharLiteral.Lex(lexer);

        Assert.NotNull(lexed);
        Assert.Equal(literal.ToArray(), lexed.Sourcecode.ToArray());
        Assert.Equal(literal.Length, lexed.SourceIndex);
    }
}
