using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Delegate : ParsingTests
{
    [Fact(DisplayName = "one parameter")]
    public void OneParameter()
    {
        // dave => { return 3; }

        List<Token> tokens = new()
        {
            Word("dave"),
            Returns(),
            StartScope(),
            Word("return"),
            Number(3),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var @delegate = Ronin.Grammar.Delegate.Parse(ref parser);

        Assert.Single(@delegate?.Data);
        var datum = @delegate.Data[0];
        Assert.Single(datum?.Name?.Components);

        Assert.Single(@delegate.Definition?.Values);
        var line = @delegate.Definition?.Values[0] as Ronin.Grammar.Reference;
        Assert.Equal(2, line.Components?.Count);

        Ronin.Grammar.Words name = line.Components[0];
        Assert.Equal(1, name?.Source.Length);
        
        Anonymous scalar = line.Components[1];
        Assert.Equal(1, scalar?.Source.Length);        
    }

    [Fact(DisplayName = "one parameter typed")]
    public void OneParameterTyped()
    {
        // (dave => money) => { return 3; }

        List<Token> tokens = new()
        {
            StartValues(),
            Word("dave"),
            Returns(),
            Word("money"),
            EndValues(),
            Returns(),
            StartScope(),
            Word("return"),
            Number(3),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var @delegate = Ronin.Grammar.Delegate.Parse(ref parser);

        Assert.Single(@delegate?.Data);
        var datum = @delegate.Data[0];
        Assert.Single(datum?.Name?.Components);

        Assert.Single(@delegate.Definition?.Values);
        var line = @delegate.Definition?.Values[0] as Ronin.Grammar.Reference;
        Assert.Equal(2, line.Components?.Count);

        {
            Ronin.Grammar.Words name = line.Components[0];
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

        List<Token> tokens = new()
        {
            StartValues(),
            Word("dave"),
            Separator(),
            Word("billy"),
            Separator(),
            Word("wanda"),
            EndValues(),
            Returns(),
            StartScope(),
            Word("return"),
            Number(3),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var @delegate = Ronin.Grammar.Delegate.Parse(ref parser);

        Assert.Equal(3, @delegate?.Data?.Count);

        Assert.Single(@delegate.Data[0]?.Name?.Components);
        Assert.Single(@delegate.Data[1]?.Name?.Components);
        Assert.Single(@delegate.Data[2]?.Name?.Components);

        Assert.Single(@delegate.Definition?.Values);
        var line = @delegate.Definition?.Values[0] as Ronin.Grammar.Reference;
        Assert.Equal(2, line.Components?.Count);

        {
            Ronin.Grammar.Words name = line.Components[0];
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

        List<Token> tokens = new()
        {
            StartValues(),
            EndValues(),
            Returns(),
            StartScope(),
            Word("return"),
            Number(3),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var @delegate = Ronin.Grammar.Delegate.Parse(ref parser);

        Assert.Empty(@delegate?.Data);

        Assert.Single(@delegate?.Definition?.Values);
        var line = @delegate.Definition?.Values[0] as Ronin.Grammar.Reference;
        Assert.Equal(2, line.Components?.Count);

        {
            Ronin.Grammar.Words name = line.Components[0];
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

        List<Token> tokens = new()
        {
            Constant(),
            Word("x"),
            Assign(),
            StartValues(),
            EndValues(),
            Returns(),
            StartScope(),
            Word("return"),
            Number(3),
            Terminal(),
            EndScope(),
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
