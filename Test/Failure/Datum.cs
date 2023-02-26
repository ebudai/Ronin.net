using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class datum
{
    [Fact(DisplayName = $"{Reactive.keyword} before name")]
    public void ReturnsBeforeName()
    {
        // reactive => 44.3;

        Token[] tokens = 
        {
            new Reactive(),
            new Returns(),
            new Number(),
            new Terminal()
        };
        
        Parser parser = new(tokens);
        var datum = DatumDeclarationSyntax.Parse(ref parser);
        
        Assert.Null(datum);
    }

    [Fact(DisplayName = "literal instead of identifier")]
    public void LiteralInsteadOfIdentifier()
    {
        // var 555;

        Token[] tokens = 
        {
            new Variable(),
            new Number(),
            new Terminal()
        };
        
        Parser parser = new(tokens);
        var datum = DatumDeclarationSyntax.Parse(ref parser);
        
        Assert.Null(datum);
    }
}

