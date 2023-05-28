using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
public class DatumDeclaration
{
    [Fact(DisplayName = $"{Reactive.keyword} before name")]
    public void ReturnsBeforeName()
    {
        // reactive => 44.3;

        Token[] tokens = 
        {
            new Reactive { sourcecode = Reactive.keyword.AsMemory() },
            new Returns(),
            new NumberLiteral { sourcecode = "44.3".AsMemory() },
            new Terminal(),
        };
        
        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);
        
        Assert.Null(datum);
    }

    [Fact(DisplayName = "literal instead of identifier")]
    public void LiteralInsteadOfIdentifier()
    {
        // var 555;

        Token[] tokens = 
        {
            new Variable { sourcecode = Variable.keyword.AsMemory() },
            new NumberLiteral { sourcecode = "555".AsMemory() },
            new Terminal ()
        };
        
        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);
        
        Assert.Null(datum);
    }
}

