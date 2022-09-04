using Ronin.Compiler;

namespace Failure;

public class Whitespace
{
    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Ronin.Tokens.Whitespace.Lex(lexer);

        Assert.Null(lexed);
    }
}
