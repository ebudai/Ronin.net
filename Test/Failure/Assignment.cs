using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class assignment
{
    [Fact(DisplayName = "no value")]
    public void NoValue()
    {
        Tokens tokens = new();
        tokens.Add<Word>("x")
            .Add<Assign>()
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var assignment = Assignment.Parse(ref parser);
        Assert.Null(assignment);
    }
}
