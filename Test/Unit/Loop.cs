using Ronin;
using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keyword;
using Ronin.Lexicon.Punctuation;

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
            new OpenBrace(),
            new Word(),
            new Word(),
            new Assign(),
            new NumberLiteral(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var loop = Ronin.Grammar.Loop.Parse(ref parser);

        Assert.Equal(3, loop?.Header?.Name?.Source.Length);
        
        Assert.Single(loop.Body?.Values);
        var assignment = loop.Body.Values[0] as Ronin.Grammar.Assignment;
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
            new OpenBrace(),
            new Word(),
            new Plus(),
            new Plus(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var loop = Ronin.Grammar.Loop.Parse(ref parser);

        Assert.NotNull(loop?.Header?.Datatype);
    }
}
