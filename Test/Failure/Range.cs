using Ronin.Compiler;
using Range = Ronin.Lexicon.Symbols.Range;

namespace Failure;

[Trait("Lexer", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class range
{
    [Fact(DisplayName = "not a range")]
    public void NotRange()
    {
        const string literal = "notARange";

        Lexer lexer = new(literal);
        var lexed = Range.Lex(ref lexer);

        Assert.IsNotType<Range>(lexed);
    }
}
