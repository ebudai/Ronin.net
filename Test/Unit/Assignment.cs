using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class assignment
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // a = 3;
        
        Token[] tokens =
        {
            new Word(),
            new Assign(),
            new Number(),
            new Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var assignment = Assignment.Parse(ref parser);

        Assert.Single(assignment?.Reference?.Components);
        Name name = assignment.Reference.Components[0];
        Assert.Single(name?.Words);

        Scalar scalar = assignment.Value;
        Assert.Single(scalar?.Literals);
    }

    [Fact(DisplayName = "no whitespace")]
    public void NoWhitespace()
    {
        // thing = 0

        Token[] tokens =
        {
            new Word(),
            new Assign(),
            new Number(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var assignment = Assignment.Parse(ref parser);

        Assert.Single(assignment?.Reference?.Components);
        Name name = assignment.Reference.Components?[0];
        Assert.Single(name?.Words);
        
        Scalar scalar = assignment.Value;
        Assert.Single(scalar?.Literals);
    }
}
