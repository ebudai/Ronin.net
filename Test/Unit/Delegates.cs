using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

using Delegate = Ronin.Grammar.Delegate;
using Literal = Ronin.Grammar.Literal;

namespace Unit;

[Trait(nameof(Parser), null)]
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
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var @delegate = Delegate.Parse(ref parser);

        {
            Assert.Single(@delegate?.Data);
            var name = @delegate.Data[0].AsName;
            Assert.Single(name?.Tokens.ToArray());
        }

        Assert.Single(@delegate.Definition.Statements);
        var unresolved = @delegate.Definition.Statements[0] as Member.Unresolved;
        Assert.Equal(2, unresolved?.Reference.Span.Length);

        {
            var name = unresolved.Reference.Span[0].AsName;
            Assert.Single(name?.Tokens.ToArray());
        }
        
        var scalar = unresolved.Reference.Span[1].AsTemporary as Literal;
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
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var @delegate = Delegate.Parse(ref parser);

        Assert.Single(@delegate?.Data);
        var datum = @delegate.Data[0].AsDatum;
        Assert.Single(datum?.Identifier);

        Assert.Single(@delegate.Definition.Statements);
        var unresolved = @delegate.Definition.Statements[0] as Member.Unresolved;
        Assert.Equal(2, unresolved?.Reference.Span.Length);

        {
            var name = unresolved.Reference.Span[0].AsName;
            Assert.Single(name?.Tokens.ToArray());
        }

        {
            var scalar = unresolved.Reference.Span[1].AsTemporary as Literal;
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
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var @delegate = Delegate.Parse(ref parser);

        Assert.Equal(3, @delegate?.Data.Count);

        var identifier = @delegate.Data[0].AsName;
        Assert.Single(identifier?.Tokens.ToArray());
        identifier = @delegate.Data[1].AsName;
        Assert.Single(identifier?.Tokens.ToArray());
        identifier = @delegate.Data[2].AsName;
        Assert.Single(identifier?.Tokens.ToArray());

        Assert.Single(@delegate.Definition.Statements);
        var unresolved = @delegate.Definition.Statements[0] as Member.Unresolved;
        Assert.Equal(2, unresolved?.Reference.Span.Length);

        {
            var name = unresolved.Reference.Span[0].AsName;
            Assert.Single(name?.Tokens.ToArray());
        }

        {
            var scalar = unresolved.Reference.Span[1].AsTemporary as Literal;
            Assert.Single(scalar?.Tokens.ToArray());
        }
    }

    [Fact(DisplayName = "empty parameters")]
    public void EmptyParameters()
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
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var @delegate = Delegate.Parse(ref parser);

        Assert.Empty(@delegate?.Data);

        Assert.Single(@delegate?.Definition.Statements);
        var unresolved = @delegate.Definition.Statements[0] as Member.Unresolved;
        Assert.Equal(2, unresolved?.Reference.Span.Length);

        {
            var name = unresolved.Reference.Span[0].AsName;
            Assert.Single(name?.Tokens.ToArray());
        }

        {
            var scalar = unresolved.Reference.Span[1].AsTemporary as Literal;
            Assert.Single(scalar?.Tokens.ToArray());
        }
    }

    [Fact(DisplayName = "as value")]
    public void AsValue()
    {
        // var x = () => { return 3; }

        List<Token> tokens = new()
        {
            Keyword.Variable(),
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
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var datum = Datum.Parse(ref parser);

        var @delegate = datum?.Initializer as Delegate;
        Assert.NotNull(@delegate);
    }
}
