using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Loop : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // for each car in cars { car speed = 9000; }

        List<Token> tokens = new()
        {
            ForEach(),
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
        var loop = Ronin.Grammar.Loop.Parse(ref parser);

        Assert.Single(loop?.Header?.Name?.Components);
        Ronin.Grammar.Words name = loop.Header.Name.Components[0];
        Assert.Equal(3, name?.Source.Length);
        
        Assert.Single(loop.Definition?.Values);
        var assignment = loop.Definition.Values[0] as Ronin.Grammar.Assignment;
        Assert.NotNull(assignment);
    }

    [Fact(DisplayName = "specifies datatype")]
    public void SpecifiesDatatype()
    {
        // for each var value => whole number in values { value++; }
        
        List<Token> tokens = new()
        {
            ForEach(),
            Variable(),
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
        var loop = Ronin.Grammar.Loop.Parse(ref parser);

        Assert.NotNull(loop?.Header?.Datatype);
    }
}
