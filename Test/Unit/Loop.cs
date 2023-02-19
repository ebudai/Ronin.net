using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class loop
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // for each car in cars { car speed = 9000; }

        Token[] tokens =
        {
            new ForEach(),
            new Word(),
            new In(),
            new Word(),
            new OpenBrace(),
            new Word(),
            new Word(),
            new Assign(),
            new Number(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var loop = Loop.Parse(ref parser);

        Assert.False(loop?.Mutable);

        Assert.Single(loop.Variable?.Words);
        
        Reference reference = loop.List;
        Assert.Single(reference?.Components);
        Name name = reference.Components[0];
        Assert.Single(name?.Words);
        
        Assert.Single(loop.Body?.Values);
        Assignment assignment = loop.Body.Values[0];
        Assert.NotNull(assignment);
    }
}
