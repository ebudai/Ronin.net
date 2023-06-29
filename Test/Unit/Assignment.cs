using Ronin.Compiler;
using Ronin.Lexicon;
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

        Assert.IsType<Assign>(assignment.Type);

        var scalar = assignment.Value as Ronin.Grammar.Literal;
        Assert.Equal(1, scalar?.Source.Length);
    }

    [Fact(DisplayName = "add assign")]
    public void AddAssignTest()
    {
        // stuff += 3;

        List<Token> tokens = new()
        {
            Word("stuff"),
            AddAssign(),
            Number(3),
            Terminal()
        };

        Parser parser = new(tokens);
        var assignment = Ronin.Grammar.Assignment.Parse(ref parser);

        Assert.Single(assignment?.Reference?.Components);
        Ronin.Grammar.Words name = assignment.Reference.Components?[0];
        Assert.Equal(1, name?.Source.Length);

        Assert.IsType<AddAssign>(assignment.Type);

        var scalar = assignment.Value as Ronin.Grammar.Literal;
        Assert.Equal(1, scalar?.Source.Length);
    }

    [Fact(DisplayName = "and assign")]
    public void AndAssignTest()
    {
        // stuff &= no;

        List<Token> tokens = new()
        {
            Word("stuff"),
            AndAssign(),
            Word("no"),
            Terminal()
        };

        Parser parser = new(tokens);
        var assignment = Ronin.Grammar.Assignment.Parse(ref parser);

        Assert.Single(assignment?.Reference?.Components);
        Ronin.Grammar.Words name = assignment.Reference.Components?[0];
        Assert.Equal(1, name?.Source.Length);

        Assert.IsType<AndAssign>(assignment.Type);

        var reference = assignment.Value as Ronin.Grammar.Reference;
        Assert.Equal(1, reference?.Source.Length);
    }

    [Fact(DisplayName = "divide assign")]
    public void DivideAssignTest()
    {
        // stuff /= 8.2;

        List<Token> tokens = new()
        {
            Word("stuff"),
            DivideAssign(),
            Number(8.2),
            Terminal()
        };

        Parser parser = new(tokens);
        var assignment = Ronin.Grammar.Assignment.Parse(ref parser);

        Assert.Single(assignment?.Reference?.Components);
        Ronin.Grammar.Words name = assignment.Reference.Components?[0];
        Assert.Equal(1, name?.Source.Length);

        Assert.IsType<DivideAssign>(assignment.Type);

        var scalar = assignment.Value as Ronin.Grammar.Literal;
        Assert.Equal(1, scalar?.Source.Length);
    }

    [Fact(DisplayName = "multiply assign")]
    public void MultiplyAssignTest()
    {
        // stuff *= 9.66;

        List<Token> tokens = new()
        {
            Word("stuff"),
            MultiplyAssign(),
            Number(9.66),
            Terminal()
        };

        Parser parser = new(tokens);
        var assignment = Ronin.Grammar.Assignment.Parse(ref parser);

        Assert.Single(assignment?.Reference?.Components);
        Ronin.Grammar.Words name = assignment.Reference.Components?[0];
        Assert.Equal(1, name?.Source.Length);

        Assert.IsType<MultiplyAssign>(assignment.Type);

        var scalar = assignment.Value as Ronin.Grammar.Literal;
        Assert.Equal(1, scalar?.Source.Length);
    }

    [Fact(DisplayName = "or assign")]
    public void OrAssignTest()
    {
        // stuff |= yes;

        List<Token> tokens = new()
        {
            Word("stuff"),
            OrAssign(),
            Word("yes"),
            Terminal()
        };

        Parser parser = new(tokens);
        var assignment = Ronin.Grammar.Assignment.Parse(ref parser);

        Assert.Single(assignment?.Reference?.Components);
        Ronin.Grammar.Words name = assignment.Reference.Components?[0];
        Assert.Equal(1, name?.Source.Length);

        Assert.IsType<OrAssign>(assignment.Type);

        var reference = assignment.Value as Ronin.Grammar.Reference;
        Assert.Equal(1, reference?.Source.Length);
    }

    [Fact(DisplayName = "subtract assign")]
    public void SubtractAssignTest()
    {
        // stuff -= 12;

        List<Token> tokens = new()
        {
            Word("stuff"),
            SubtractAssign(),
            Number(12),
            Terminal()
        };

        Parser parser = new(tokens);
        var assignment = Ronin.Grammar.Assignment.Parse(ref parser);

        Assert.Single(assignment?.Reference?.Components);
        Ronin.Grammar.Words name = assignment.Reference.Components?[0];
        Assert.Equal(1, name?.Source.Length);

        Assert.IsType<SubtractAssign>(assignment.Type);

        var scalar = assignment.Value as Ronin.Grammar.Literal;
        Assert.Equal(1, scalar?.Source.Length);
    }
}
