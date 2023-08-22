using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Assignments : ParsingTests
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
        var assignment = Assignment.Parse(ref parser);

        var unresolved = assignment?.Destination as Datum.Unresolved;
        
        Assert.Single(unresolved?.Reference.Components);
        Name name = unresolved.Reference.Components[0];
        Assert.Single(name?.Source.ToArray());

        var scalar = assignment.Value as Inline;
        Assert.Single(scalar?.Source.ToArray());
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
        var assignment = Assignment.Parse(ref parser);

        var unresolved = assignment?.Destination as Datum.Unresolved;

        Assert.Single(unresolved?.Reference.Components);
        Name name = unresolved.Reference.Components?[0];
        Assert.Single(name?.Source.ToArray());

        Assert.IsType<Assign>(assignment.Type);

        var scalar = assignment.Value as Inline;
        Assert.Single(scalar?.Source.ToArray());
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
        var assignment = Assignment.Parse(ref parser);

        var unresolved = assignment?.Destination as Datum.Unresolved;

        Assert.Single(unresolved?.Reference.Components);
        Name name = unresolved.Reference.Components?[0];
        Assert.Single(name?.Source.ToArray());

        Assert.IsType<AddAssign>(assignment.Type);

        var scalar = assignment.Value as Inline;
        Assert.Single(scalar?.Source.ToArray());
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
        var assignment = Assignment.Parse(ref parser);

        var unresolvedDatum = assignment?.Destination as Datum.Unresolved;

        Assert.Single(unresolvedDatum?.Reference.Components);
        Name name = unresolvedDatum.Reference.Components?[0];
        Assert.Single(name?.Source.ToArray());

        Assert.IsType<AndAssign>(assignment.Type);

        var unresolvedValue = assignment.Value as Value.Unresolved;
        Assert.Single(unresolvedValue?.Reference?.Source.ToArray());
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
        var assignment = Assignment.Parse(ref parser);

        var unresolved = assignment?.Destination as Datum.Unresolved;

        Assert.Single(unresolved?.Reference.Components);
        Name name = unresolved.Reference.Components?[0];
        Assert.Single(name?.Source.ToArray());

        Assert.IsType<DivideAssign>(assignment.Type);

        var scalar = assignment.Value as Inline;
        Assert.Single(scalar?.Source.ToArray());
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
        var assignment = Assignment.Parse(ref parser);

        var unresolved = assignment?.Destination as Datum.Unresolved;

        Assert.Single(unresolved?.Reference.Components);
        Name name = unresolved.Reference.Components?[0];
        Assert.Single(name?.Source.ToArray());

        Assert.IsType<MultiplyAssign>(assignment.Type);

        var scalar = assignment.Value as Inline;
        Assert.Single(scalar?.Source.ToArray());
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
        var assignment = Assignment.Parse(ref parser);

        var unresolvedDatum = assignment?.Destination as Datum.Unresolved;

        Assert.Single(unresolvedDatum?.Reference.Components);
        Name name = unresolvedDatum.Reference.Components?[0];
        Assert.Single(name?.Source.ToArray());

        Assert.IsType<OrAssign>(assignment.Type);

        var unresolvedValue = assignment.Value as Value.Unresolved;
        Assert.Single(unresolvedValue?.Reference?.Source.ToArray());
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
        var assignment = Assignment.Parse(ref parser);

        var unresolved = assignment?.Destination as Datum.Unresolved;

        Assert.Single(unresolved?.Reference.Components);
        Name name = unresolved.Reference.Components?[0];
        Assert.Single(name?.Source.ToArray());

        Assert.IsType<SubtractAssign>(assignment.Type);

        var scalar = assignment.Value as Inline;
        Assert.Single(scalar?.Source.ToArray());
    }
}
