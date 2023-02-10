using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using System.Security.Cryptography.X509Certificates;
using Test;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class list
{
    [Fact(DisplayName = "single")]
    public void Single()
    {
        Tokens tokens = new();
        tokens.Add<OpenBrace>()
            .Add<Number>("3")
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        var list = List.Parse(ref parser);

        Assert.Single(list?.Values);
        Scalar scalar = list.Values[0];
        Assert.Single(scalar?.Literals);
        Assert.Equal("3", scalar.Literals[0]?.ToString());
    }

    [Fact(DisplayName = "multiple")]
    public void Multiple()
    {
        const string one = "1";
        const string two = "2";
        const string six = "6";

        Tokens tokens = new();
        tokens.Add<OpenBrace>()
            .Add<Number>(one)
            .Add<Separator>()
            .Add<Number>(two)
            .Add<Separator>()
            .Add<Number>(six)
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        var list = List.Parse(ref parser);

        Assert.Equal(3, list?.Values?.Count);

        {
            Scalar scalar = list.Values[0];
            Assert.Single(scalar?.Literals);
            Assert.Equal(one, scalar.Literals[0]?.ToString());
        }

        {
            Scalar scalar = list.Values[1];
            Assert.Single(scalar?.Literals);
            Assert.Equal(two, scalar.Literals[0]?.ToString());
        }

        {
            Scalar scalar = list.Values[2];
            Assert.Single(scalar?.Literals);
            Assert.Equal(six, scalar.Literals[0]?.ToString());
        }
    }

    [Fact(DisplayName = "as value")]
    public void AsValue()
    {
        const string thing = "thing";
        const string one = "1";
        const string two = "2";
        const string stuff = "stuff";

        Tokens tokens = new();
        tokens.Add<Variable>()
            .Add<Word>(thing)
            .Add<Assign>()
            .Add<OpenBrace>()
            .Add<Number>(one)
            .Add<Separator>()
            .Add<Number>(two)
            .Add<Separator>()
            .Add<Word>(stuff)
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        var statements = parser.Parse();

        Assert.Single(statements);
        Datum datum = statements[0];
        List list = datum?.Initializer;
        Assert.NotNull(list);
    }
}
