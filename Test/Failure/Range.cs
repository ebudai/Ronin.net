using Ronin.Compiler;

namespace Failure;

[Trait("Lexer", null)]
public class Range
{
    [Fact(DisplayName = "not a range")]
    public void NotRange()
    {
        const string literal = "notARange";

        Lexer lexer = new(literal);
        var lexed = Ronin.Lexicon.Punctuation.Range.Lex(ref lexer);

        Assert.IsNotType<Ronin.Lexicon.Punctuation.Range>(lexed);
    }
}
