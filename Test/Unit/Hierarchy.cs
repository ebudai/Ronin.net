using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class hierarchy
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // part of things;

        Token[] tokens =
        {
            new PartOf(),
            new Word(),
            new Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var hierarchy = Hierarchy.Parse(ref parser);

        Assert.IsType<PartOf>(hierarchy?.Direction);

        Assert.Single(hierarchy.Components);
        Name name = hierarchy.Components[0];
        Assert.Single(name?.Words);     
    }

    [Fact(DisplayName = "with some hierarchy")]
    public void WithHierarchy()
    {
        // import standard funstuff websockets;

        Token[] tokens =
        {
            new Import(),
            new Word(),
            new Word(),
            new Word(),
            new Terminal(),
            Sentinel.Instance
        };
                
        Parser parser = new(tokens);
        var hierarchy = Hierarchy.Parse(ref parser);

        Assert.IsType<Import>(hierarchy?.Direction);
        Assert.Single(hierarchy.Components);
        Name name = hierarchy.Components[0];
        Assert.Equal(3, name?.Words?.Count);
    }

    [Fact(DisplayName = "keywords are just text")]
    public void WithKeywords()
    {
        // part of thing compiled to whatever secret stuff;

        Token[] tokens =
        {
            new PartOf(),
            new Word(),
            new Compiled(),
            new Word(),
            new Word(),
            new Word(),
            new Word(),
            new Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var hierarchy = Hierarchy.Parse(ref parser);

        Assert.IsType<PartOf>(hierarchy?.Direction);
        Assert.Single(hierarchy.Components);
        Name name = hierarchy.Components[0];
        Assert.Equal(6, name?.Words?.Count);
    }

    [Fact(DisplayName = "using text literal")]
    public void TextLiteral()
    {
        // part of literal testing "fast version" readonly;

        Token[] tokens =
        {
            new PartOf(),
            new Word(),
            new Word(),
            new Text(),
            new Word(),
            new Terminal(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var hierarchy = Hierarchy.Parse(ref parser);

        Assert.IsType<PartOf>(hierarchy?.Direction);

        Assert.Equal(3, hierarchy.Components?.Count);

        {
            Name name = hierarchy.Components[0];
            Assert.Equal(2, name?.Words?.Count);
        }

        {
            Scalar scalar = hierarchy.Components[1];
            Assert.Single(scalar?.Literals);            
        }

        {
            Name name = hierarchy.Components[2];
            Assert.Single(name?.Words);
        }
    }
}