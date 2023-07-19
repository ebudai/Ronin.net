using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Loops : ParsingTests
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

        Parser parser = new(tokens);
        var loop = Loop.Parse(ref parser);

        Assert.Equal(3, loop.Header.Name?.Source.Length);
        
        Assert.Single(loop.Definition?.Values);
        var assignment = loop.Definition.Values[0] as Assignment;
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

        Parser parser = new(tokens);
        var loop = Loop.Parse(ref parser);

        Assert.NotNull(loop?.Header?.Datatype);
    }
}
