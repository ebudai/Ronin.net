using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;

namespace Unit;

[Trait("Parser", null)]
public class Export
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // part of things;

        Token[] tokens =
        {
            new PartOf(),
            new Word(),
            new Terminal { sourcecode = new[] { Terminal.symbol } },
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var export = Ronin.Grammar.Export.Parse(ref parser);

        Assert.Equal(1, export.Name?.Source.Length);     
    }

    [Fact(DisplayName = "with some hierarchy")]
    public void WithExport()
    {
        // part of standard funstuff websockets;

        Token[] tokens =
        {
            new PartOf(),
            new Word(),
            new Word(),
            new Word(),
            new Terminal { sourcecode = new[] { Terminal.symbol } },
            Sentinel.Instance
        };
                
        Parser parser = new(tokens);
        var export = Ronin.Grammar.Export.Parse(ref parser);

        Assert.Equal(3, export.Name?.Source.Length);
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
            new Terminal { sourcecode = new[] { Terminal.symbol } },
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var export = Ronin.Grammar.Export.Parse(ref parser);

        Assert.Equal(6, export.Name?.Source.Length);
    }
}