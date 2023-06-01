using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
public class DatumDeclarations
{
    [Fact(DisplayName = $"{Reactive.keyword} before name")]
    public void ReturnsBeforeName()
    {
        // reactive => 44.3;

        Token[] tokens = 
        {
            new Reactive { sourcecode = Reactive.keyword.AsMemory() },
            new Returns { sourcecode = Returns.symbol.AsMemory() },
            new Number { sourcecode = "44.3".AsMemory() },
            new Terminal { sourcecode = new[] { Terminal.symbol } },
        };
        
        Parser parser = new(tokens);
        var datum = DatumDeclaration.Parse(ref parser);
        
        Assert.Null(datum);
    }

    [Fact(DisplayName = "literal instead of identifier")]
    public void LiteralInsteadOfIdentifier()
    {
        // var 555;

        Token[] tokens = 
        {
            new Variable { sourcecode = Variable.keyword.AsMemory() },
            new Number { sourcecode = "555".AsMemory() },
            new Terminal { sourcecode = new[] { Terminal.symbol } },
        };
        
        Parser parser = new(tokens);
        var datum = DatumDeclaration.Parse(ref parser);
        
        Assert.Null(datum);
    }
}

