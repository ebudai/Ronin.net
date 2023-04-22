using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keyword;
using Ronin.Lexicon.Punctuation;

namespace Unit;

[Trait("Parser", null)]
public class Datatype
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // datatype Test { }

        Token[] tokens = 
        {
            new Ronin.Lexicon.Keyword.Datatype(),
            new Word(),
            new OpenBrace(),
            new CloseBrace(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datatype = Ronin.Grammar.Datatype.Parse(ref parser);

        Assert.Single(datatype?.Identifier?.Components);
        Ronin.Grammar.Name name = datatype.Identifier.Components[0];
        Assert.Equal(1, name?.Source.Length);
    }

    [Fact(DisplayName = "with algebra and members")]
    public void Algebra()
    {
        // datatype Algebra Example = number or { var cash => money; var debt => money; }

        Token[] tokens =
        {
            new Ronin.Lexicon.Keyword.Datatype(),
            new Word(),
            new Word(),
            new Assign(),
            new Word(),
            new Word(),
            new OpenBrace(),
            new Variable(),
            new Word(),
            new Returns(),
            new Word(),
            new Terminal(),
            new Variable(),
            new Word(),
            new Returns(),
            new Word(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datatype = Ronin.Grammar.Datatype.Parse(ref parser);

        Assert.Single(datatype?.Identifier?.Components);
        Ronin.Grammar.Name algebra = datatype.Algebra.Components[0];
        Assert.Equal(2, algebra?.Source.Length);
        
        Assert.Equal(2, datatype.Body?.Values.Count);

        {
            Ronin.Grammar.Datum cash = datatype.Body.Values[0];
            Assert.IsType<Variable>(cash?.Mutability);
            Assert.Equal(1, cash.Name?.Source.Length);
            Assert.Single(cash.Datatype?.Components);
            Ronin.Grammar.Name type = cash.Datatype.Components[0];
            Assert.Equal(1, type?.Source.Length);
        }

        {
            Ronin.Grammar.Datum debt = datatype.Body.Values[1];
            Assert.IsType<Variable>(debt?.Mutability);
            Assert.Equal(1, debt.Name?.Source.Length);
            Assert.Single(debt.Datatype?.Components);
            Ronin.Grammar.Name type = debt.Datatype.Components[0];
            Assert.Equal(1, type?.Source.Length);
        }        
    }
}
