using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

using Delegate = Ronin.Grammar.Delegate;
using Literal = Ronin.Grammar.Literal;

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

        Parser parser = new(tokens.AsLinkedList());
        var @delegate = Delegate.Parse(ref parser);

        {
            Assert.Single(@delegate?.Data);
            var name = @delegate.Data[0].AsT1;
            Assert.Single(name?.Tokens.ToArray());
        }

        Assert.Single(@delegate.Definition);
        var unresolved = @delegate.Definition[0] as Member.Unresolved;
        Assert.Equal(2, unresolved?.Reference.Components.Count);

        {
            var name = unresolved.Reference.Components[0].AsT0;
            Assert.Single(name?.Tokens.ToArray());
        }
        
        var scalar = unresolved.Reference.Components[1].AsT1 as Literal;
        Assert.Single(scalar?.Tokens.ToArray());        
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

        Parser parser = new(tokens.AsLinkedList());
        var @delegate = Delegate.Parse(ref parser);

        Assert.Single(@delegate?.Data);
        Datum datum = @delegate.Data[0].AsT0;
        Assert.Single(datum?.Identifier);

        Assert.Single(@delegate.Definition);
        var unresolved = @delegate.Definition[0] as Member.Unresolved;
        Assert.Equal(2, unresolved?.Reference.Components.Count);

        {
            var name = unresolved.Reference.Components[0].AsT0;
            Assert.Single(name?.Tokens.ToArray());
        }

        {
            var scalar = unresolved.Reference.Components[1].AsT1 as Literal;
            Assert.Single(scalar?.Tokens.ToArray());
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
        
        Parser parser = new(tokens.AsLinkedList());
        var @delegate = Delegate.Parse(ref parser);

        Assert.Equal(3, @delegate?.Data.Count);

        var identifier = @delegate.Data[0].AsT1;
        Assert.Single(identifier?.Tokens.ToArray());
        identifier = @delegate.Data[1].AsT1;
        Assert.Single(identifier?.Tokens.ToArray());
        identifier = @delegate.Data[2].AsT1;
        Assert.Single(identifier?.Tokens.ToArray());

        Assert.Single(@delegate.Definition);
        var unresolved = @delegate.Definition[0] as Member.Unresolved;
        Assert.Equal(2, unresolved?.Reference.Components.Count);

        {
            Name name = unresolved.Reference.Components[0].AsT0;
            Assert.Single(name?.Tokens.ToArray());
        }

        {
            var scalar = unresolved.Reference.Components[1].AsT1 as Literal;
            Assert.Single(scalar?.Tokens.ToArray());
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
        
        Parser parser = new(tokens.AsLinkedList());
        var @delegate = Delegate.Parse(ref parser);

        Assert.Empty(@delegate?.Data);

        Assert.Single(@delegate?.Definition);
        var unresolved = @delegate.Definition[0] as Member.Unresolved;
        Assert.Equal(2, unresolved?.Reference.Components.Count);

        {
            Name name = unresolved.Reference.Components[0].AsT0;
            Assert.Single(name?.Tokens.ToArray());
        }

        {
            var scalar = unresolved.Reference.Components[1].AsT1 as Literal;
            Assert.Single(scalar?.Tokens.ToArray());
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
        
        Parser parser = new(tokens.AsLinkedList());
        var statements = parser.Parse().ToList();

        Assert.Single(statements);
        var datum = statements[0] as Datum;
        var @delegate = datum?.Initializer as Delegate;
        Assert.NotNull(@delegate);
    }
}
