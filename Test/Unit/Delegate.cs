using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keyword;
using Ronin.Lexicon.Punctuation;

namespace Unit;

[Trait("Parser", null)]
public class Delegate
{
    [Fact(DisplayName = "one parameter")]
    public void OneParameter()
    {
        // dave => { return 3; }

        Token[] tokens =
        {
            new Word { sourcecode = "dave".AsMemory() },
            new Returns { sourcecode = Returns.symbol.AsMemory() },
            new OpenBrace { sourcecode = OpenBrace.symbol.AsMemory() },
            new Word { sourcecode = "return".AsMemory() },
            new NumberLiteral { sourcecode = "3".AsMemory() },
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            new CloseBrace { sourcecode = CloseBrace.symbol.AsMemory() },
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var @delegate = Ronin.Grammar.Delegate.Parse(ref parser);

        Assert.Single(@delegate?.Data);
        var datum = @delegate.Data[0];
        Assert.Single(datum?.Name?.Components);

        Assert.Single(@delegate.Body?.Values);
        var line = @delegate.Body?.Values[0] as Ronin.Grammar.Reference;
        Assert.Equal(2, line.Components?.Count);

        Ronin.Grammar.Name name = line.Components[0];
        Assert.Equal(1, name?.Source.Length);
        
        Anonymous scalar = line.Components[1];
        Assert.Equal(1, scalar?.Source.Length);        
    }

    [Fact(DisplayName = "one parameter typed")]
    public void OneParameterTyped()
    {
        // (dave => money) => { return 3; }

        Token[] tokens =
        {
            new OpenParenthesis { sourcecode = OpenParenthesis.symbol.AsMemory() },
            new Word { sourcecode = "dave".AsMemory() },
            new Returns { sourcecode = Returns.symbol.AsMemory() },
            new Word { sourcecode = "money".AsMemory() },
            new OpenBrace { sourcecode = OpenBrace.symbol.AsMemory() },
            new Word { sourcecode = "money".AsMemory() },
            new NumberLiteral(),
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            new CloseBrace { sourcecode = CloseBrace.symbol.AsMemory() },
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var @delegate = Ronin.Grammar.Delegate.Parse(ref parser);

        Assert.Single(@delegate?.Data);
        var datum = @delegate.Data[0];
        Assert.Single(datum?.Name?.Components);

        Assert.Single(@delegate.Body?.Values);
        var line = @delegate.Body?.Values[0] as Ronin.Grammar.Reference;
        Assert.Equal(2, line.Components?.Count);

        {
            Ronin.Grammar.Name name = line.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Anonymous scalar = line.Components[1];
            Assert.Equal(1, scalar?.Source.Length);
        }
    }

    [Fact(DisplayName = "three parameters")]
    public void ThreeParameters()
    {
        // (dave, billy, wanda) => { return 3; }

        Token[] tokens =
        {
            new OpenParenthesis(),
            new Word(),
            new Separator(),
            new Word(),
            new Separator(),
            new Word(),
            new CloseParenthesis(),
            new Returns { sourcecode = Returns.symbol.AsMemory() },
            new OpenBrace { sourcecode = OpenBrace.symbol.AsMemory() },
            new Word(),
            new NumberLiteral(),
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            new CloseBrace { sourcecode = CloseBrace.symbol.AsMemory() },
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var @delegate = Ronin.Grammar.Delegate.Parse(ref parser);

        Assert.Equal(3, @delegate?.Data?.Count);

        Assert.Single(@delegate.Data[0]?.Name?.Components);
        Assert.Single(@delegate.Data[1]?.Name?.Components);
        Assert.Single(@delegate.Data[2]?.Name?.Components);

        Assert.Single(@delegate.Body?.Values);
        var line = @delegate.Body?.Values[0] as Ronin.Grammar.Reference;
        Assert.Equal(2, line.Components?.Count);

        {
            Ronin.Grammar.Name name = line.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Anonymous scalar = line.Components[1];
            Assert.Equal(1, scalar?.Source.Length);
        }
    }

    [Fact(DisplayName = "no parameters")]
    public void NoParameters()
    {
        // () => { return 3; }

        Token[] tokens =
        {
            new OpenParenthesis(),
            new CloseParenthesis(),
            new Returns { sourcecode = Returns.symbol.AsMemory() },
            new OpenBrace { sourcecode = OpenBrace.symbol.AsMemory() },
            new Word(),
            new NumberLiteral(),
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            new CloseBrace { sourcecode = CloseBrace.symbol.AsMemory() },
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var @delegate = Ronin.Grammar.Delegate.Parse(ref parser);

        Assert.Empty(@delegate?.Data);

        Assert.Single(@delegate?.Body?.Values);
        var line = @delegate.Body?.Values[0] as Ronin.Grammar.Reference;
        Assert.Equal(2, line.Components?.Count);

        {
            Ronin.Grammar.Name name = line.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Anonymous scalar = line.Components[1];
            Assert.Equal(1, scalar?.Source.Length);
        }
    }

    [Fact(DisplayName = "as value")]
    public void AsValue()
    {
        // constant x = () => { return 3; }

        Token[] tokens = 
        {
            new Constant(),
            new Word(),
            new Assign(),
            new OpenParenthesis(),
            new CloseParenthesis(),
            new Returns { sourcecode = Returns.symbol.AsMemory() },
            new OpenBrace { sourcecode = OpenBrace.symbol.AsMemory() },
            new Word(),
            new NumberLiteral(),
            new Terminal { sourcecode = Terminal.symbol.AsMemory() },
            new CloseBrace { sourcecode = CloseBrace.symbol.AsMemory() },
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var statements = parser.Parse().Values;

        Assert.Single(statements);
        var datum = statements[0] as Ronin.Grammar.DatumDeclaration;
        var @delegate = datum?.Initializer as Ronin.Grammar.Delegate;
        Assert.NotNull(@delegate);
    }
}
