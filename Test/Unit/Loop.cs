using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

namespace Unit;

[Trait("Parser", null)]
public class Loop
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // for each car in cars { car speed = 9000; }

        Token[] tokens =
        {
            new ForEach(),
            new Word(),
            new Word(),
            new Word(),
            new StartScope(),
            new Word(),
            new Word(),
            new Assign(),
            new Number(),
            new Terminal(),
            new EndScope(),
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
        
        Token[] tokens =
        {
            new ForEach(),
            new Variable(),
            new Word(),
            new Returns(),
            new Word(),
            new Word(),
            new Word(),
            new Word(),
            new StartScope(),
            new Word(),
            new Ronin.Lexicon.Symbol(),
            new Ronin.Lexicon.Symbol(),
            new Terminal(),
            new EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var loop = Ronin.Grammar.Loop.Parse(ref parser);

        Assert.NotNull(loop?.Header?.Datatype);
    }
}
