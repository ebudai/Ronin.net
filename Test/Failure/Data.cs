using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait(nameof(Parser), null)]
public class Data : ParsingTests
{
    [Fact(DisplayName = $"{Reactive.keyword} is the name")]
    public void ReturnsBeforeName()
    {
        // reactive => 44.3;

        List<Token> tokens = new()
        {
            Keyword.Reactive(),
            Returns(),
            Number(44.3),
            Terminal(),
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var datum = Datum.Parse(ref parser);
        
        Assert.Null(datum);
    }

    [Fact(DisplayName = "literal instead of identifier")]
    public void LiteralInsteadOfIdentifier()
    {
        // var 555;

        List<Token> tokens = new()
        {
            Keyword.Variable(),
            Number(555),
            Terminal(),
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var datum = Datum.Parse(ref parser);
        
        Assert.IsType<Datum.ExpectedIdentifierError>(datum);
    }
}

