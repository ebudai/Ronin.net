using Ronin.Compiler;
using Ronin.Grammar;
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
            new AssignSymbol(),
            new TerminalSymbol()
        };
        
        Parser parser = new(tokens);
        var assignment = AssignmentSyntax.Parse(ref parser);
        
        Assert.Null(assignment);
    }
}
