using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Test;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class hierarchy
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        Tokens tokens = new();
        tokens.Add<PartOf>()
            .Add<Word>("standard")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var hierarchy = Hierarchy.Parse(ref parser);

        Assert.NotNull(hierarchy);

        Assert.IsType<PartOf>(hierarchy.Direction);

        Assert.Single(hierarchy.Components);
        Name name = hierarchy.Components[0];
        Assert.Single(name.Words);
        Assert.Equal("standard", name.Words[0]);        
    }

    [Fact(DisplayName = "with some hierarchy")]
    public void WithHierarchy()
    {
        Tokens tokens = new();
        tokens.Add<Import>()
            .Add<Word>("standard")
            .Add<Word>("funstuff")
            .Add<Word>("websockets")
            .Add<Terminal>();
                
        Parser parser = new(tokens.ToArray());
        var hierarchy = Hierarchy.Parse(ref parser);

        Assert.NotNull(hierarchy);

        Assert.IsType<Import>(hierarchy.Direction);

        Assert.Single(hierarchy.Components);
        Name name = hierarchy.Components[0];
        Assert.Equal(3, name.Words.Count);
        Assert.Equal("standard", name.Words[0]);
        Assert.Equal("funstuff", name.Words[1]);
        Assert.Equal("websockets", name.Words[2]);        
    }

    [Fact(DisplayName = "keywords are just text")]
    public void WithKeywords()
    {
        Tokens tokens = new();
        tokens.Add<PartOf>()
            .Add<Word>("thing")
            .Add<Word>("compiled")
            .Add<Word>("to")
            .Add<Word>("whatever")
            .Add<Word>("secret")
            .Add<Word>("stuff")
            .Add<Terminal>();
        
        Parser parser = new(tokens.ToArray());
        var hierarchy = Hierarchy.Parse(ref parser);

        Assert.NotNull(hierarchy);

        Assert.IsType<PartOf>(hierarchy.Direction);

        Assert.Single(hierarchy.Components);
        Name name = hierarchy.Components[0];
        Assert.Equal(6, name.Words.Count);
        Assert.Equal("thing", name.Words[0]);
        Assert.Equal("compiled", name.Words[1]);
        Assert.Equal("to", name.Words[2]);
        Assert.Equal("whatever", name.Words[3]);
        Assert.Equal("secret", name.Words[4]);
        Assert.Equal("stuff", name.Words[5]);        
    }

    [Fact(DisplayName = "using text literal")]
    public void TextLiteral()
    {
        Tokens tokens = new();
        tokens.Add<PartOf>()
            .Add<Word>("literal")
            .Add<Word>("testing")
            .Add<Text>("\"fast version\"")
            .Add<Word>("readonly")
            .Add<Terminal>();
        
        Parser parser = new(tokens.ToArray());
        var hierarchy = Hierarchy.Parse(ref parser);

        Assert.NotNull(hierarchy);

        Assert.IsType<PartOf>(hierarchy.Direction);

        Assert.Equal(3, hierarchy.Components.Count);

        Name name = hierarchy.Components[0];
        Assert.Equal(2, name.Words.Count);
        Assert.Equal("literal", name.Words[0]);
        Assert.Equal("testing", name.Words[1]);

        Scalar scalar = hierarchy.Components[1];
        Assert.Single(scalar.Literals);
        Assert.Equal("\"fast version\"", scalar.Literals[0].ToString());

        name = hierarchy.Components[2];
        Assert.Single(name.Words);
        Assert.Equal("readonly", name.Words[0]);
    }
}