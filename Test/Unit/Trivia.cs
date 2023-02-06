using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class trivia
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        Tokens tokens = new();
        tokens.Add<Whitespace>().Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var trivia = Trivia.Parse(ref parser);
        Assert.NotNull(trivia);
    }
}
