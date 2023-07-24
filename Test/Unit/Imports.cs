using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

using Import = Ronin.Grammar.Import;

namespace Unit;

[Trait("Parser", null)]
public class Imports : ParsingTests  
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // import things;

        List<Token> tokens = new()
        {
            Keyword.Import(),
            Word("things"),
            Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var import = Import.Parse(ref parser);

        Assert.Equal(1, import.Name?.Source.Length);     
    }

    [Fact(DisplayName = "with some hierarchy")]
    public void WithExport()
    {
        // import standard funstuff websockets;

        List<Token> tokens = new()
        {
            Keyword.Import(),
            Word("standard"),
            Word("funstuff"),
            Word("websockets"),
            Terminal(),
            Sentinel.Instance
        };
                
        Parser parser = new(tokens);
        var import = Import.Parse(ref parser);

        Assert.Equal(3, import.Name?.Source.Length);
    }

    [Fact(DisplayName = "keywords are just text")]
    public void WithKeywords()
    {
        // import thing compiled to whatever secret stuff;

        List<Token> tokens = new()
        {
            Keyword.Import(),
            Word("thing"),
            Keyword.Compiled(),
            Word("to"),
            Word("whatever"),
            Word("secret"),
            Word("stuff"),
            Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var import = Import.Parse(ref parser);

        Assert.Equal(6, import.Name?.Source.Length);
    }
}