using Ronin.Compiler;

namespace Failure;

public class Whitespace
{
    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new() { Sourcecode = string.Empty.ToArray() };
        var lexed = Ronin.Tokens.Whitespace.Lex(lexer);

        Assert.Null(lexed);
    }
}
