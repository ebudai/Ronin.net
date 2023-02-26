using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;

namespace Failure;

[Trait("Parser", null)]
public class Lookup
{
    [Fact(DisplayName = "missing assign")]
    public void MissingAssign()
    {
        // { "thing" 4 }

        Token[] tokens =
        {
            new OpenBraceSymbol(),
            new TextLiteral(),
            new NumberLiteral(),
            new CloseBraceSymbol()
        };
        
        Parser parser = new(tokens);
        var lookup = InlineLookupSyntax.Parse(ref parser);

        Assert.IsNotType<InlineLookupSyntax>(lookup);
    }

    [Fact(DisplayName = "missing key")]
    public void MissingKey()
    {
        // { = 4 }

        Token[] tokens =
        {
            new OpenBraceSymbol(),
            new AssignSymbol(),
            new NumberLiteral(),
            new CloseBraceSymbol()
        };
        
        Parser parser = new(tokens);
        var lookup = InlineLookupSyntax.Parse(ref parser);

        Assert.IsNotType<InlineLookupSyntax>(lookup);
    }

    [Fact(DisplayName = "missing value")]
    public void MissingValue()
    {
        // { 3 = }

        Token[] tokens =
        {
            new OpenBraceSymbol(),
            new NumberLiteral(),
            new AssignSymbol(),
            new CloseBraceSymbol()
        };
        
        Parser parser = new(tokens);
        var lookup = InlineLookupSyntax.Parse(ref parser);

        Assert.IsNotType<InlineLookupSyntax>(lookup);
    }
}
