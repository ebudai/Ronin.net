using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keyword;
using Ronin.Lexicon.Punctuation;

namespace Unit;

[Trait("Parser", null)]
public class DatatypeDeclaration
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // datatype Test { }

        Token[] tokens =
        {
            new Datatype { sourcecode = Datatype.keyword.AsMemory() },
            new Word { sourcecode = "Test".AsMemory() },
            new OpenBrace { sourcecode = OpenBrace.symbol.AsMemory() },
            new CloseBrace { sourcecode = CloseBrace.symbol.AsMemory() },
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var datatype = Ronin.Grammar.DatatypeDeclaration.Parse(ref parser);

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
            new Datatype { sourcecode = Datatype.keyword.AsMemory() },
            new Word { sourcecode = "Algebra".AsMemory() },
            new Word { sourcecode = "Example".AsMemory() },
            new Assign { sourcecode = Assign.symbol.AsMemory() },
            new Word { sourcecode = "number".AsMemory() },
            new Word { sourcecode = "or".AsMemory() },
            new OpenBrace { sourcecode = OpenBrace.symbol.AsMemory() },
            new Variable { sourcecode = Variable.keyword.AsMemory() },
            new Word { sourcecode = "cash".AsMemory() },
            new Returns { sourcecode = Returns.symbol.AsMemory() },
            new Word { sourcecode = "money".AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            new Variable { sourcecode = Variable.keyword.AsMemory() },
            new Word { sourcecode = "debt".AsMemory() },
            new Returns { sourcecode = Returns.symbol.AsMemory() },
            new Word { sourcecode = "money".AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            new CloseBrace { sourcecode = CloseBrace.symbol.AsMemory() },
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var datatype = Ronin.Grammar.DatatypeDeclaration.Parse(ref parser);

        Assert.Single(datatype?.Identifier?.Components);
        Ronin.Grammar.Name algebra = datatype.Algebra.Components[0];
        Assert.Equal(2, algebra?.Source.Length);
        
        Assert.Equal(2, datatype.Body?.Values.Count);

        {
            var cash = datatype.Body.Values[0] as Ronin.Grammar.DatumDeclaration;
            Assert.IsType<Variable>(cash?.Mutability);
            Assert.Single(cash.Name?.Components);
            Assert.Single(cash.Datatype?.Components);
            Ronin.Grammar.Name type = cash.Datatype.Components[0];
            Assert.Equal(1, type?.Source.Length);
        }

        {
            var debt = datatype.Body.Values[1] as Ronin.Grammar.DatumDeclaration;
            Assert.IsType<Variable>(debt?.Mutability);
            Assert.Single(debt.Name?.Components);
            Assert.Single(debt.Datatype?.Components);
            Ronin.Grammar.Name type = debt.Datatype.Components[0];
            Assert.Equal(1, type?.Source.Length);
        }        
    }
}
