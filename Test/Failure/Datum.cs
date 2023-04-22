using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keyword;
using Ronin.Lexicon.Punctuation;

namespace Failure;

[Trait("Parser", null)]
public class Datum
{
    [Fact(DisplayName = $"{Reactive.keyword} before name")]
    public void ReturnsBeforeName()
    {
        // reactive => 44.3;

        Token[] tokens = 
        {
            new Reactive(),
            new Returns(),
            new NumberLiteral(),
            new Terminal()
        };
        
        Parser parser = new(tokens);
        var datum = Ronin.Grammar.Datum.Parse(ref parser);
        
        Assert.Null(datum);
    }

    [Fact(DisplayName = "literal instead of identifier")]
    public void LiteralInsteadOfIdentifier()
    {
        // var 555;

        Token[] tokens = 
        {
            new Variable(),
            new NumberLiteral(),
            new Terminal()
        };
        
        Parser parser = new(tokens);
        var datum = Ronin.Grammar.Datum.Parse(ref parser);
        
        Assert.Null(datum);
    }
}

