using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon.Literals;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Assignment
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        TokensGenerator tokens = new();
        tokens.Add<Ronin.Lexicon.Word>("x")
            .Add<Ronin.Lexicon.Whitespace>()
            .Add<Assign>()
            .Add<Ronin.Lexicon.Whitespace>()
            .Add<Number>("17")
            .Add<Terminal>();

        Parser parser = new(tokens.Tokens.ToArray());
        var assignment = Ronin.Grammar.Assignment.Parse(ref parser);

        Assert.NotNull(assignment);

        Assert.NotNull(assignment.Reference);
        Assert.Single(assignment.Reference.Components);

        Ronin.Grammar.Name name = assignment.Reference.Components[0];        
        Assert.Single(name.Words);
        Assert.Equal("x", name.Words[0]);

        Temporary value = assignment.Value;
        Assert.NotNull(value);

        Ronin.Grammar.Scalar scalar = value;
        Assert.NotNull(scalar);
        Assert.Single(scalar.Literals);
        Assert.Equal("17", scalar.Literals[0].ToString());
    }

    [Fact(DisplayName = "no whitespace")]
    public void NoWhitespace()
    {
        TokensGenerator tokens = new();
        tokens.Add<Ronin.Lexicon.Word>("x")
            .Add<Assign>()
            .Add<Number>("17")
            .Add<Terminal>();

        Parser parser = new(tokens.Tokens.ToArray());
        var assignment = Ronin.Grammar.Assignment.Parse(ref parser);

        Assert.NotNull(assignment);

        Assert.NotNull(assignment.Reference);
        Assert.Single(assignment.Reference.Components);

        Ronin.Grammar.Name name = assignment.Reference.Components[0];
        Assert.Single(name.Words);
        Assert.Equal("x", name.Words[0]);

        Temporary value = assignment.Value;
        Assert.NotNull(value);

        Ronin.Grammar.Scalar scalar = value;
        Assert.NotNull(scalar);
        Assert.Single(scalar.Literals);
        Assert.Equal("17", scalar.Literals[0].ToString());
    }
}
