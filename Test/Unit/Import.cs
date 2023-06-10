using Ronin.Compiler;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Import : ParsingTests  
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // import things;

        List<Token> tokens = new()
        {
            Import(),
            Word("things"),
            Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var import = Ronin.Grammar.Import.Parse(ref parser);

        Assert.Equal(1, import.Name?.Source.Length);     
    }

    [Fact(DisplayName = "with some hierarchy")]
    public void WithExport()
    {
        // import standard funstuff websockets;

        List<Token> tokens = new()
        {
            Import(),
            Word("standard"),
            Word("funstuff"),
            Word("websockets"),
            Terminal(),
            Sentinel.Instance
        };
                
        Parser parser = new(tokens);
        var import = Ronin.Grammar.Import.Parse(ref parser);

        Assert.Equal(3, import.Name?.Source.Length);
    }

    [Fact(DisplayName = "keywords are just text")]
    public void WithKeywords()
    {
        // import thing compiled to whatever secret stuff;

        List<Token> tokens = new()
        {
            Import(),
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
        var import = Ronin.Grammar.Import.Parse(ref parser);

        Assert.Equal(6, import.Name?.Source.Length);
    }
}