using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class assignment
{
    [Fact(DisplayName = "no value")]
    public void NoValue()
    {
        Token[] tokens = 
        {
            new Word(),
            new Assign(),
            new Terminal()
        };
        
        Parser parser = new(tokens);
        var assignment = Assignment.Parse(ref parser);
        
        Assert.Null(assignment);
    }
}
