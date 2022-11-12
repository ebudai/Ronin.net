using Ronin.Compiler;

namespace Failure;

[Trait("Lexer", null)]
public class Whitespace
{
    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Ronin.Lexicon.Whitespace.Lex(lexer);

        Assert.Null(lexed);
    }
}
