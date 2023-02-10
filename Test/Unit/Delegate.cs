using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

using Delegate = Ronin.Grammar.Delegate;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable IDE1006
public class @delegate
{
    [Fact(DisplayName = "one parameter")]
    public void OneParameter()
    {
        const string dave = "dave";
        const string @return = "return";
        const string three = "3";

        Tokens tokens = new();
        tokens.Add<Word>(dave)
            .Add<Returns>()
            .Add<OpenBrace>()
            .Add<Word>(@return)
            .Add<Number>(three)
            .Add<Terminal>()
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        var @delegate = Delegate.Parse(ref parser);

        Assert.Single(@delegate?.Data);
        Datum datum = @delegate.Data[0];
        Assert.Single(datum?.Name?.Words);
        Assert.Equal(dave, datum.Name.Words[0]);

        Assert.Single(@delegate.Body?.Values);
        Value value = @delegate.Body?.Values[0];
        Reference line = value;
        Assert.Equal(2, line.Components?.Count);

        {
            Name name = line.Components[0];
            Assert.Single(name?.Words);
            Assert.Equal(@return, name.Words[0]);
        }

        {
            Scalar scalar = line.Components[1];
            Assert.Single(scalar?.Literals);
            Assert.Equal(three, scalar.Literals[0]?.ToString());
        }
    }

    [Fact(DisplayName = "three parameters")]
    public void ThreeParameters()
    {
        const string dave = "dave";
        const string billy = "billy";
        const string wanda = "wanda";
        const string @return = "return";
        const string three = "3";

        Tokens tokens = new();
        tokens.Add<OpenParenthesis>()
            .Add<Word>(dave)
            .Add<Separator>()
            .Add<Word>(billy)
            .Add<Separator>()
            .Add<Word>(wanda)
            .Add<CloseParenthesis>()
            .Add<Returns>()
            .Add<OpenBrace>()
            .Add<Word>(@return)
            .Add<Number>(three)
            .Add<Terminal>()
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        var @delegate = Delegate.Parse(ref parser);

        Assert.Equal(3, @delegate?.Data?.Count);

        Assert.Single(@delegate.Data[0]?.Name?.Words);
        Assert.Equal(dave, @delegate.Data[0].Name.Words[0]);

        Assert.Single(@delegate.Data[1]?.Name?.Words);
        Assert.Equal(billy, @delegate.Data[1].Name.Words[0]);

        Assert.Single(@delegate.Data[2]?.Name?.Words);
        Assert.Equal(wanda, @delegate.Data[2].Name.Words[0]);

        Assert.Single(@delegate.Body?.Values);
        Value value = @delegate.Body?.Values[0];
        Reference line = value;
        Assert.Equal(2, line.Components?.Count);

        {
            Name name = line.Components[0];
            Assert.Single(name?.Words);
            Assert.Equal(@return, name.Words[0]);
        }

        {
            Scalar scalar = line.Components[1];
            Assert.Single(scalar?.Literals);
            Assert.Equal(three, scalar.Literals[0]?.ToString());
        }
    }

    [Fact(DisplayName = "no parameters")]
    public void NoParameters()
    {
        const string @return = "return";
        const string three = "3";

        Tokens tokens = new();
        tokens.Add<OpenBrace>()
            .Add<Word>(@return)
            .Add<Number>(three)
            .Add<Terminal>()
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        var @delegate = Delegate.Parse(ref parser);

        Assert.Null(@delegate?.Data);

        Assert.Single(@delegate?.Body?.Values);
        Value value = @delegate.Body?.Values[0];
        Reference line = value;
        Assert.Equal(2, line.Components?.Count);

        {
            Name name = line.Components[0];
            Assert.Single(name?.Words);
            Assert.Equal(@return, name.Words[0]);
        }

        {
            Scalar scalar = line.Components[1];
            Assert.Single(scalar?.Literals);
            Assert.Equal(three, scalar.Literals[0]?.ToString());
        }
    }

    [Fact(DisplayName = "as value")]
    public void AsValue()
    {
        const string x = "x";
        const string @return = "return";
        const string three = "3";

        Tokens tokens = new();
        tokens.Add<Constant>()
            .Add<Word>(x)
            .Add<Assign>()
            .Add<OpenBrace>()
            .Add<Word>(@return)
            .Add<Number>(three)
            .Add<Terminal>()
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        var statements = parser.Parse();

        Assert.Single(statements);
        Datum datum = statements[0];
        Delegate @delegate = datum?.Initializer;
        Assert.NotNull(@delegate);
    }
}
