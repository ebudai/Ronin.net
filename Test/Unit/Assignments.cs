using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;
using Association = Ronin.Grammar.Association;
using Literal = Ronin.Grammar.Literal;

namespace Unit;

[Trait(nameof(Parser), null)]
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
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var assignment = Association.Parse(ref parser);

        var unresolved = assignment?.Destination as Member.Unresolved;
        
        Assert.Single(unresolved?.Reference);
        var name = unresolved.Reference.Span[0].AsName;
        Assert.Single(name?.Tokens.ToArray());

        var scalar = assignment.Origin as Literal;
        Assert.Single(scalar?.Tokens.ToArray());
    }

    [Fact(DisplayName = "no whitespace")]
    public void NoWhitespace()
    {
        // thing=0

        List<Token> tokens = new()
        {
            Word("thing"),
            Assign(),
            Number(0),
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var association = Association.Parse(ref parser);

        var unresolved = association?.Destination as Member.Unresolved;

        Assert.Single(unresolved?.Reference);
        var name = unresolved.Reference.Span[0].AsName;
        Assert.Single(name?.Tokens.ToArray());

        Assert.IsType<Assign>(association.Assignment);

        var scalar = association.Origin as Literal;
        Assert.Single(scalar?.Tokens.ToArray());
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

        Parser parser = new(tokens.AsLinkedList());
        var association = Association.Parse(ref parser);

        var unresolved = association?.Destination as Member.Unresolved;

        Assert.Single(unresolved?.Reference);
        var name = unresolved.Reference.Span[0].AsName;
        Assert.Single(name?.Tokens.ToArray());

        Assert.IsType<AddAssign>(association.Assignment);

        var scalar = association.Origin as Literal;
        Assert.Single(scalar?.Tokens.ToArray());
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

        Parser parser = new(tokens.AsLinkedList());
        var association = Association.Parse(ref parser);

        var unresolvedDatum = association?.Destination as Member.Unresolved;

        Assert.Single(unresolvedDatum?.Reference);
        var name = unresolvedDatum.Reference.Span[0].AsName;
        Assert.Single(name?.Tokens.ToArray());

        Assert.IsType<AndAssign>(association.Assignment);

        var member = association.Origin as Member.Unresolved;
        Assert.Single(member?.Reference);
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

        Parser parser = new(tokens.AsLinkedList());
        var association = Association.Parse(ref parser);

        var unresolved = association?.Destination as Member.Unresolved;

        Assert.Single(unresolved?.Reference);
        var name = unresolved.Reference.Span[0].AsName;
        Assert.Single(name?.Tokens.ToArray());

        Assert.IsType<DivideAssign>(association.Assignment);

        var scalar = association.Origin as Literal;
        Assert.Single(scalar?.Tokens.ToArray());
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

        Parser parser = new(tokens.AsLinkedList());
        var association = Association.Parse(ref parser);

        var unresolved = association?.Destination as Member.Unresolved;

        Assert.Single(unresolved?.Reference);
        var name = unresolved.Reference.Span[0].AsName;
        Assert.Single(name?.Tokens.ToArray());

        Assert.IsType<MultiplyAssign>(association.Assignment);

        var scalar = association.Origin as Literal;
        Assert.Single(scalar?.Tokens.ToArray());
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

        Parser parser = new(tokens.AsLinkedList());
        var association = Association.Parse(ref parser);

        var unresolvedDatum = association?.Destination as Member.Unresolved;

        Assert.Single(unresolvedDatum?.Reference);
        var name = unresolvedDatum.Reference.Span[0].AsName;
        Assert.Single(name?.Tokens.ToArray());

        Assert.IsType<OrAssign>(association.Assignment);

        var member = association.Origin as Member.Unresolved;
        Assert.Single(member?.Reference);
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

        Parser parser = new(tokens.AsLinkedList());
        var association = Association.Parse(ref parser);

        var unresolved = association?.Destination as Member.Unresolved;

        Assert.Single(unresolved?.Reference);
        var name = unresolved.Reference.Span[0].AsName;
        Assert.Single(name?.Tokens.ToArray());

        Assert.IsType<SubtractAssign>(association.Assignment);

        var scalar = association.Origin as Literal;
        Assert.Single(scalar?.Tokens.ToArray());
    }
}
