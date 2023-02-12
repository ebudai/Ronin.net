using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;
using Test;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class assignment
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        Tokens tokens = new();
        tokens.Add<Ronin.Lexicon.Word>("x")
            .Add<Assign>()
            .Add<Number>("17")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var assignment = Assignment.Parse(ref parser);

        Assert.Single(assignment?.Reference?.Components);
        Name name = assignment.Reference.Components[0];
        Assert.Single(name?.Words);
        Assert.Equal("x", name.Words[0]);

        Scalar scalar = assignment.Value;
        Assert.Single(scalar?.Literals);
        Assert.Equal("17", scalar.Literals[0]?.Sourcecode.ToString());
    }

    [Fact(DisplayName = "no whitespace")]
    public void NoWhitespace()
    {
        Tokens tokens = new();
        tokens.Add<Word>("x")
            .Add<Assign>()
            .Add<Number>("17")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var assignment = Assignment.Parse(ref parser);

        Assert.Single(assignment?.Reference?.Components);
        Name name = assignment.Reference.Components?[0];
        Assert.Single(name?.Words);
        Assert.Equal("x", name.Words[0]);

        Scalar scalar = assignment.Value;
        Assert.Single(scalar?.Literals);
        Assert.Equal("17", scalar.Literals[0]?.Sourcecode.ToString());
    }
}
