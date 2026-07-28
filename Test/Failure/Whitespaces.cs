using Ronin;
using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

[Trait(nameof(Lexer), null)]
public class Whitespaces
{
    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Whitespace.Lex(ref lexer);

        Assert.Null(lexed);
    }
}
