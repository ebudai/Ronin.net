using Ronin.Compiler;
using Ronin.Lexicon;
using Test;
using Type = Ronin.Grammar.Type;

namespace Failure;

[Trait(nameof(Parser), null)]
public class Algebras : ParsingTests
{
    [Fact(DisplayName = "missing")]
    public void Missing()
    {
        // type thing = { }

        List<Token> tokens = new()
        {
            Keyword.Type(),
            Word("thing"),
            Assign(),
            StartScope(),
            EndScope(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var type = Type.Parse(ref parser);

        Assert.NotNull(type);
        Assert.Null(type.Algebra);
    }
}
