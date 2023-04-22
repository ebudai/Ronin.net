using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

[Trait("Parser", null)]
public class Assignment
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
        var assignment = Ronin.Grammar.Assignment.Parse(ref parser);
        
        Assert.Null(assignment);
    }
}
