using Ronin.Compiler;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Name : ParsingTests
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
        var name = Ronin.Grammar.Words.Parse(ref parser);

        Assert.Equal(3, name?.Source.Length);
    }
}
