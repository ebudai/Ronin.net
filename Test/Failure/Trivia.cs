using Ronin.Compiler;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait(nameof(Parser), null)]
public class Trivia : ParsingTests
{
    [Fact(DisplayName = "not trivia")]
    public void Basic()
    {
        List<Token> tokens = new() { Word("stuff"), new Sentinel() };

        Parser parser = new(tokens.AsLinkedList());
        var parsed = Ronin.Grammar.Trivia.Parse(ref parser);

        Assert.Null(parsed);
    }
}
