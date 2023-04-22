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
            new Word(),
            new Returns(),
            new OpenBrace(),
            new Word(),
            new NumberLiteral(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var @delegate = Ronin.Grammar.Delegate.Parse(ref parser);

        Assert.Single(@delegate?.Data);
        Ronin.Grammar.Datum datum = @delegate.Data[0];
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
            Ronin.Grammar.Literal scalar = line.Components[1];
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
            new Returns(),
            new OpenBrace(),
            new Word(),
            new NumberLiteral(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var @delegate = Ronin.Grammar.Delegate.Parse(ref parser);

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
            Ronin.Grammar.Literal scalar = line.Components[1];
            Assert.Equal(1, scalar?.Source.Length);
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
            new NumberLiteral(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var @delegate = Ronin.Grammar.Delegate.Parse(ref parser);

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
            Ronin.Grammar.Literal scalar = line.Components[1];
            Assert.Equal(1, scalar?.Source.Length);
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
            new NumberLiteral(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var statements = parser.Parse().Values;

        Assert.Single(statements);
        Ronin.Grammar.Datum datum = statements[0];
        Ronin.Grammar.Delegate @delegate = datum?.Initializer;
        Assert.NotNull(@delegate);
    }
}
