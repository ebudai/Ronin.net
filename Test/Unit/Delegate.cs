using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

using DelegateSyntax = Ronin.Grammar.DelegateSyntax;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable IDE1006
public class @delegate
{
    [Fact(DisplayName = "one parameter")]
    public void OneParameter()
    {
        // dave => { return 3; }

        Token[] tokens =
        {
            new Word(),
            new Returns(),
            new OpenBrace(),
            new Word(),
            new Number(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var @delegate = DelegateSyntax.Parse(ref parser);

        Assert.Single(@delegate?.Data);
        DatumDeclarationSyntax datum = @delegate.Data[0];
        Assert.Single(datum?.Name?.Source);

        Assert.Single(@delegate.Body?.Values);
        Value value = @delegate.Body?.Values[0];
        Reference line = value;
        Assert.Equal(2, line.Components?.Count);

        {
            Name name = line.Components[0];
            Assert.Single(name?.Source);
        }

        {
            LiteralSyntax scalar = line.Components[1];
            Assert.Single(scalar?.Source);
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
            new Returns(),
            new OpenBrace(),
            new Word(),
            new Number(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var @delegate = DelegateSyntax.Parse(ref parser);

        Assert.Equal(3, @delegate?.Data?.Count);

        Assert.Single(@delegate.Data[0]?.Name?.Source);
        Assert.Single(@delegate.Data[1]?.Name?.Source);
        Assert.Single(@delegate.Data[2]?.Name?.Source);

        Assert.Single(@delegate.Body?.Values);
        Value value = @delegate.Body?.Values[0];
        Reference line = value;
        Assert.Equal(2, line.Components?.Count);

        {
            Name name = line.Components[0];
            Assert.Single(name?.Source);
        }

        {
            LiteralSyntax scalar = line.Components[1];
            Assert.Single(scalar?.Source);
        }
    }

    [Fact(DisplayName = "no parameters")]
    public void NoParameters()
    {
        // { return 3; }

        Token[] tokens =
        {
            new OpenBrace(),
            new Word(),
            new Number(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var @delegate = DelegateSyntax.Parse(ref parser);

        Assert.Null(@delegate?.Data);

        Assert.Single(@delegate?.Body?.Values);
        Value value = @delegate.Body?.Values[0];
        Reference line = value;
        Assert.Equal(2, line.Components?.Count);

        {
            Name name = line.Components[0];
            Assert.Single(name?.Source);
        }

        {
            LiteralSyntax scalar = line.Components[1];
            Assert.Single(scalar?.Source);
        }
    }

    [Fact(DisplayName = "as value")]
    public void AsValue()
    {
        // constant x = { return 3; }

        Token[] tokens = 
        {
            new Constant(),
            new Word(),
            new Assign(),
            new OpenBrace(),
            new Word(),
            new Number(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var statements = parser.Parse();

        Assert.Single(statements);
        DatumDeclarationSyntax datum = statements[0];
        DelegateSyntax @delegate = datum?.Initializer;
        Assert.NotNull(@delegate);
    }
}
