using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Unit;

[Trait("Parser", null)]
public class Assignment
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // a = 3;
        
        Token[] tokens =
        {
            new Word(),
            new AssignSymbol(),
            new NumberLiteral(),
            new TerminalSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var assignment = AssignmentSyntax.Parse(ref parser);

        Assert.Single(assignment?.Reference?.Components);
        Ronin.Grammar.Name name = assignment.Reference.Components[0];
        Assert.Equal(1, name?.Source.Length);

        LiteralSyntax scalar = assignment.Value;
        Assert.Equal(1, scalar?.Source.Length);
    }

    [Fact(DisplayName = "no whitespace")]
    public void NoWhitespace()
    {
        // thing = 0

        Token[] tokens =
        {
            new Word(),
            new AssignSymbol(),
            new NumberLiteral(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var assignment = AssignmentSyntax.Parse(ref parser);

        Assert.Single(assignment?.Reference?.Components);
        Ronin.Grammar.Name name = assignment.Reference.Components?[0];
        Assert.Equal(1, name?.Source.Length);
        
        LiteralSyntax scalar = assignment.Value;
        Assert.Equal(1, scalar?.Source.Length);
    }
}
