using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

using DelegateSyntax = Ronin.Grammar.DelegateSyntax;

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
            new Word(),
            new ReturnsSymbol(),
            new OpenBraceSymbol(),
            new Word(),
            new NumberLiteral(),
            new TerminalSymbol(),
            new CloseBraceSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var @delegate = DelegateSyntax.Parse(ref parser);

        Assert.Single(@delegate?.Data);
        DatumDeclarationSyntax datum = @delegate.Data[0];
        Assert.Equal(1, datum?.Name?.Source.Length);

        Assert.Single(@delegate.Body?.Values);
        Value value = @delegate.Body?.Values[0];
        Ronin.Grammar.Reference line = value;
        Assert.Equal(2, line.Components?.Count);

        {
            Ronin.Grammar.Name name = line.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            LiteralSyntax scalar = line.Components[1];
            Assert.Equal(1, scalar?.Source.Length);
        }
    }

    [Fact(DisplayName = "three parameters")]
    public void ThreeParameters()
    {
        // (dave, billy, wanda) => { return 3; }

        Token[] tokens =
        {
            new OpenParenthesisSymbol(),
            new Word(),
            new SeparatorSymbol(),
            new Word(),
            new SeparatorSymbol(),
            new Word(),
            new CloseParenthesisSymbol(),
            new ReturnsSymbol(),
            new OpenBraceSymbol(),
            new Word(),
            new NumberLiteral(),
            new TerminalSymbol(),
            new CloseBraceSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var @delegate = DelegateSyntax.Parse(ref parser);

        Assert.Equal(3, @delegate?.Data?.Count);

        Assert.Equal(1, @delegate.Data[0]?.Name?.Source.Length);
        Assert.Equal(1, @delegate.Data[1]?.Name?.Source.Length);
        Assert.Equal(1, @delegate.Data[2]?.Name?.Source.Length);

        Assert.Single(@delegate.Body?.Values);
        Value value = @delegate.Body?.Values[0];
        Ronin.Grammar.Reference line = value;
        Assert.Equal(2, line.Components?.Count);

        {
            Ronin.Grammar.Name name = line.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            LiteralSyntax scalar = line.Components[1];
            Assert.Equal(1, scalar?.Source.Length);
        }
    }

    [Fact(DisplayName = "no parameters")]
    public void NoParameters()
    {
        // { return 3; }

        Token[] tokens =
        {
            new OpenBraceSymbol(),
            new Word(),
            new NumberLiteral(),
            new TerminalSymbol(),
            new CloseBraceSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var @delegate = DelegateSyntax.Parse(ref parser);

        Assert.Null(@delegate?.Data);

        Assert.Single(@delegate?.Body?.Values);
        Value value = @delegate.Body?.Values[0];
        Ronin.Grammar.Reference line = value;
        Assert.Equal(2, line.Components?.Count);

        {
            Ronin.Grammar.Name name = line.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            LiteralSyntax scalar = line.Components[1];
            Assert.Equal(1, scalar?.Source.Length);
        }
    }

    [Fact(DisplayName = "as value")]
    public void AsValue()
    {
        // constant x = { return 3; }

        Token[] tokens = 
        {
            new ConstantKeyword(),
            new Word(),
            new AssignSymbol(),
            new OpenBraceSymbol(),
            new Word(),
            new NumberLiteral(),
            new TerminalSymbol(),
            new CloseBraceSymbol(),
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
