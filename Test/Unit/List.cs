using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using System.Security.Cryptography.X509Certificates;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class list
{
    [Fact(DisplayName = "single")]
    public void Single()
    {
        Token[] tokens =
        {
            new OpenBrace(),
            new Number(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var list = List.Parse(ref parser);

        Assert.Single(list?.Values);
        Scalar scalar = list.Values[0];
        Assert.Single(scalar?.Source);
    }

    [Fact(DisplayName = "multiple")]
    public void Multiple()
    {
        Token[] tokens =
        {
            new OpenBrace(),
            new Number(),
            new Separator(),
            new Number(),
            new Separator(),
            new Number(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var list = List.Parse(ref parser);

        Assert.Equal(3, list?.Values?.Count);

        {
            Scalar scalar = list.Values[0];
            Assert.Single(scalar?.Source);
        }

        {
            Scalar scalar = list.Values[1];
            Assert.Single(scalar?.Source);
        }

        {
            Scalar scalar = list.Values[2];
            Assert.Single(scalar?.Source);
        }
    }

    [Fact(DisplayName = "as value")]
    public void AsValue()
    {
        Token[] tokens =
        {
            new Variable(),
            new Word(),
            new Assign(),
            new OpenBrace(),
            new Number(),
            new Separator(),
            new Number(),
            new Separator(),
            new Word(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var statements = parser.Parse();

        Assert.Single(statements);
        Datum datum = statements[0];
        List list = datum?.Initializer;
        Assert.NotNull(list);
    }
}
