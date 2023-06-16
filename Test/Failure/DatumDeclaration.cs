using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait("Parser", null)]
public class DatumDeclarations : ParsingTests
{
    [Fact(DisplayName = $"{Ronin.Lexicon.Keywords.Reactive.keyword} is the name")]
    public void ReturnsBeforeName()
    {
        // reactive => 44.3;

        List<Token> tokens = new()
        {
            Reactive(),
            Returns(),
            Number(44.3),
            Terminal(),
        };
        
        Parser parser = new(tokens);
        var datum = DatumDeclaration.Parse(ref parser);
        
        Assert.Null(datum);
    }

    [Fact(DisplayName = "literal instead of identifier")]
    public void LiteralInsteadOfIdentifier()
    {
        // var 555;

        List<Token> tokens = new()
        {
            Variable(),
            Number(555),
            Terminal(),
        };
        
        Parser parser = new(tokens);
        var datum = DatumDeclaration.Parse(ref parser);
        
        Assert.Null(datum);
    }
}

