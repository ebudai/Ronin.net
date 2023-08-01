using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Names : ParsingTests
{
    [Fact(DisplayName = "symbols")]
    public void Symbols()
    {
        // name + things

        List<Token> tokens = new()
        {
            Word("name"),
            Symbol("+"),
            Word("things"),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var name = Name.Parse(ref parser);

        Assert.Equal(3, name?.Source.Length);
    }

    [Fact(DisplayName = "words")]
    public void Words()
    {
        // name all the things

        List<Token> tokens = new()
        {
            Word("name"),
            Whitespace(),
            Word("all"),
            Whitespace(),
            Word("the"),
            Whitespace(),
            Word("things"),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var name = Identifier.Parse(ref parser);

        Assert.Single(name?.Components);
    }
}
