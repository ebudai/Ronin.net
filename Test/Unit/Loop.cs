using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Unit;

[Trait("Parser", null)]
public class Loop
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // for each car in cars { car speed = 9000; }

        Token[] tokens =
        {
            new ForEachKeyword(),
            new Word(),
            new Word(),
            new Word(),
            new OpenBraceSymbol(),
            new Word(),
            new Word(),
            new AssignSymbol(),
            new NumberLiteral(),
            new TerminalSymbol(),
            new CloseBraceSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var loop = LoopSyntax.Parse(ref parser);

        Assert.Equal(3, loop?.Header?.Name?.Source.Length);
        
        Assert.Single(loop.Body?.Values);
        AssignmentSyntax assignment = loop.Body.Values[0];
        Assert.NotNull(assignment);
    }

    [Fact(DisplayName = "specifies datatype")]
    public void SpecifiesDatatype()
    {
        // for each var value => whole number in values { value++; }
        
        Token[] tokens =
        {
            new ForEachKeyword(),
            new VariableKeyword(),
            new Word(),
            new ReturnsSymbol(),
            new Word(),
            new Word(),
            new Word(),
            new Word(),
            new OpenBraceSymbol(),
            new Word(),
            new PlusSymbol(),
            new PlusSymbol(),
            new TerminalSymbol(),
            new CloseBraceSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var loop = LoopSyntax.Parse(ref parser);

        Assert.NotNull(loop?.Header?.Datatype);
    }
}
