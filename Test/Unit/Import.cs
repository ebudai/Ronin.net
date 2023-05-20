using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keyword;

namespace Unit;

[Trait("Parser", null)]
public class Import
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // part of things;

        Token[] tokens =
        {
            new Ronin.Lexicon.Keyword.Import(),
            new Word(),
            new Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var import = Ronin.Grammar.Import.Parse(ref parser);

        Assert.Equal(1, import.Name?.Source.Length);     
    }

    [Fact(DisplayName = "with some hierarchy")]
    public void WithExport()
    {
        // part of standard funstuff websockets;

        Token[] tokens =
        {
            new Ronin.Lexicon.Keyword.Import(),
            new Word(),
            new Word(),
            new Word(),
            new Terminal(),
            Sentinel.Instance
        };
                
        Parser parser = new(tokens);
        var import = Ronin.Grammar.Import.Parse(ref parser);

        Assert.Equal(3, import.Name?.Source.Length);
    }

    [Fact(DisplayName = "keywords are just text")]
    public void WithKeywords()
    {
        // part of thing compiled to whatever secret stuff;

        Token[] tokens =
        {
            new Ronin.Lexicon.Keyword.Import(),
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
        var import = Ronin.Grammar.Import.Parse(ref parser);

        Assert.Equal(6, import.Name?.Source.Length);
    }
}