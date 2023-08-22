using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

using Delegate = Ronin.Grammar.Delegate;

namespace Unit;

[Trait("Parser", null)]
public class Delegates : ParsingTests
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
        var @delegate = Delegate.Parse(ref parser);

        Assert.Single(@delegate?.Data);
        var datum = @delegate.Data[0];
        Assert.Equal(1, datum?.Identifier?.Source.Length);

        Assert.Single(@delegate.Definition?.Values);
        var line = @delegate.Definition?.Values[0] as Reference;
        Assert.Equal(2, line.Components?.Count);

        Name name = line.Components[0];
        Assert.Equal(1, name?.Source.Length);
        
        AnonymousValue scalar = line.Components[1];
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
        var @delegate = Delegate.Parse(ref parser);

        Assert.Single(@delegate?.Data);
        var datum = @delegate.Data[0];
        Assert.Equal(1, datum?.Identifier?.Source.Length);

        Assert.Single(@delegate.Definition?.Values);
        var line = @delegate.Definition?.Values[0] as Reference;
        Assert.Equal(2, line.Components?.Count);

        {
            Name name = line.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            AnonymousValue scalar = line.Components[1];
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
        var @delegate = Delegate.Parse(ref parser);

        Assert.Equal(3, @delegate?.Data?.Count);

        Assert.Equal(1, @delegate.Data[0]?.Identifier?.Source.Length);
        Assert.Equal(1, @delegate.Data[1]?.Identifier?.Source.Length);
        Assert.Equal(1, @delegate.Data[2]?.Identifier?.Source.Length);

        Assert.Single(@delegate.Definition?.Values);
        var line = @delegate.Definition?.Values[0] as Reference;
        Assert.Equal(2, line.Components?.Count);

        {
            Name name = line.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            AnonymousValue scalar = line.Components[1];
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
        var @delegate = Delegate.Parse(ref parser);

        Assert.Empty(@delegate?.Data);

        Assert.Single(@delegate?.Definition?.Values);
        var line = @delegate.Definition?.Values[0] as Reference;
        Assert.Equal(2, line.Components?.Count);

        {
            Name name = line.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            AnonymousValue scalar = line.Components[1];
            Assert.Equal(1, scalar?.Source.Length);
        }
    }

    [Fact(DisplayName = "as value")]
    public void AsValue()
    {
        // constant x = () => { return 3; }

        List<Token> tokens = new()
        {
            Keyword.Constant(),
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
        var datum = statements[0] as Datum.Declaration;
        var @delegate = datum?.Initializer as Delegate;
        Assert.NotNull(@delegate);
    }
}
