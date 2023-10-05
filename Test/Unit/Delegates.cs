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
        var @delegate = Delegate.Declaration.Parse(ref parser);

        Assert.Single(@delegate?.Parameters);
        Identifier identifier = @delegate.Parameters[0];
        Assert.Equal(1, identifier?.Source.Length);

        Assert.Single(@delegate.Definition);
        var unresolved = @delegate.Definition[0] as Context.Member.Unresolved;
        Assert.Equal(2, unresolved?.Reference.Components.Count);

        Name name = unresolved.Reference.Components[0];
        Assert.Equal(1, name?.Source.Length);
        
        Value.Temporary scalar = unresolved.Reference.Components[1];
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
        var @delegate = Delegate.Declaration.Parse(ref parser);

        Assert.Single(@delegate?.Parameters);
        Datum.Declaration datum = @delegate.Parameters[0];
        Assert.Equal(1, datum?.Identifier.Source.Length);

        Assert.Single(@delegate.Definition);
        var unresolved = @delegate.Definition[0] as Context.Member.Unresolved;
        Assert.Equal(2, unresolved?.Reference.Components.Count);

        {
            Name name = unresolved.Reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Value.Temporary scalar = unresolved.Reference.Components[1];
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
        var @delegate = Delegate.Declaration.Parse(ref parser);

        Assert.Equal(3, @delegate?.Parameters.Count);

        Identifier identifier = @delegate.Parameters[0];
        Assert.Equal(1, identifier?.Source.Length);
        identifier = @delegate.Parameters[1];
        Assert.Equal(1, identifier?.Source.Length);
        identifier = @delegate.Parameters[2];
        Assert.Equal(1, identifier?.Source.Length);

        Assert.Single(@delegate.Definition);
        var unresolved = @delegate.Definition[0] as Context.Member.Unresolved;
        Assert.Equal(2, unresolved?.Reference.Components.Count);

        {
            Name name = unresolved.Reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Value.Temporary scalar = unresolved.Reference.Components[1];
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
        var @delegate = Delegate.Declaration.Parse(ref parser);

        Assert.Empty(@delegate?.Parameters);

        Assert.Single(@delegate?.Definition);
        var unresolved = @delegate.Definition[0] as Context.Member.Unresolved;
        Assert.Equal(2, unresolved?.Reference.Components.Count);

        {
            Name name = unresolved.Reference.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Value.Temporary scalar = unresolved.Reference.Components[1];
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
        var statements = parser.Parse().ToList();

        Assert.Single(statements);
        var datum = statements[0] as Datum.Declaration;
        var @delegate = datum?.Initializer as Delegate.Declaration;
        Assert.NotNull(@delegate);
    }
}
