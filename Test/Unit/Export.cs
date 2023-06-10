using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Export : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // part of things;

        List<Token> tokens = new()
        {
            PartOf(),
            Word("things"),
            Terminal(),
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

        List<Token> tokens = new()
        {
            PartOf(),
            Word("standard"),
            Word("funstuff"),
            Word("websockets"),
            Terminal(),
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

        List<Token> tokens = new()
        {
            PartOf(),
            Word("thing"),
            Compiled(),
            Word("to"),
            Word("whatever"),
            Word("secret"),
            Word("stuff"),
            Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var export = Ronin.Grammar.Export.Parse(ref parser);

        Assert.Equal(6, export.Name?.Source.Length);
    }
}