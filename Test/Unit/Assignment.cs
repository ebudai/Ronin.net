using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Assignment : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // a = 3;

        List<Token> tokens = new()
        {
            Word("a"),
            Assign(),
            Number(3),
            Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var assignment = Ronin.Grammar.Assignment.Parse(ref parser);

        Assert.Single(assignment?.Reference?.Components);
        Ronin.Grammar.Words name = assignment.Reference.Components[0];
        Assert.Equal(1, name?.Source.Length);

        var scalar = assignment.Value as Ronin.Grammar.Literal;
        Assert.Equal(1, scalar?.Source.Length);
    }

    [Fact(DisplayName = "no whitespace")]
    public void NoWhitespace()
    {
        // thing = 0

        List<Token> tokens = new()
        {
            Word("thing"),
            Assign(),
            Number(0),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var assignment = Ronin.Grammar.Assignment.Parse(ref parser);

        Assert.Single(assignment?.Reference?.Components);
        Ronin.Grammar.Words name = assignment.Reference.Components?[0];
        Assert.Equal(1, name?.Source.Length);

        var scalar = assignment.Value as Ronin.Grammar.Literal;
        Assert.Equal(1, scalar?.Source.Length);
    }
}
