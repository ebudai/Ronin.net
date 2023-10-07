using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class IteratingScopes : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // for each car in cars { car speed = 9000; }

        List<Token> tokens = new()
        {
            Keyword.ForEach(),
            Word("car"),
            Word("in"),
            Word("cars"),
            StartScope(),
            Word("car"),
            Word("speed"),
            Assign(),
            Number(9000),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens.AsLinkedList());
        var loop = Scope.Iterating.Parse(ref parser);

        Assert.NotNull(loop?.List);

        Assert.Single(loop);

        var assignment = loop[0] as Association;
        Assert.NotNull(assignment);
    }

    [Fact(DisplayName = "specifies datatype")]
    public void SpecifiesDatatype()
    {
        // for each var value => whole number in values { value++; }
        
        List<Token> tokens = new()
        {
            Keyword.ForEach(),
            Keyword.Variable(),
            Word("value"),
            Returns(),
            Word("whole"),
            Word("number"),
            Word("in"),
            Word("values"),
            StartScope(),
            Word("value"),
            Symbol("+"),
            Symbol("+"),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens.AsLinkedList());
        var loop = Scope.Iterating.Parse(ref parser);

        Assert.NotNull(loop?.List);
    }
}
