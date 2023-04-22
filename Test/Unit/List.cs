using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Compound;
using Ronin.Lexicon;
using Ronin.Lexicon.Keyword;
using Ronin.Lexicon.Punctuation;

namespace Unit;

[Trait("Parser", null)]
public class List
{
    [Fact(DisplayName = "single")]
    public void Single()
    {
        // { 3 }

        Token[] tokens =
        {
            new OpenBrace(),
            new NumberLiteral(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var list = InlineList.Parse(ref parser);

        Assert.Single(list?.Values);
        Ronin.Grammar.Literal scalar = list.Values[0];
        Assert.Equal(1, scalar?.Source.Length);
    }

    [Fact(DisplayName = "multiple")]
    public void Multiple()
    {
        // { 3, 4, 5 }

        Token[] tokens =
        {
            new OpenBrace(),
            new NumberLiteral(),
            new Separator(),
            new NumberLiteral(),
            new Separator(),
            new NumberLiteral(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var list = InlineList.Parse(ref parser);

        Assert.Equal(3, list?.Values?.Count);

        {
            Ronin.Grammar.Literal scalar = list.Values[0];
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            Ronin.Grammar.Literal scalar = list.Values[1];
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            Ronin.Grammar.Literal scalar = list.Values[2];
            Assert.Equal(1, scalar?.Source.Length);
        }
    }

    [Fact(DisplayName = "as value")]
    public void AsValue()
    {
        // var x = { 5, 2, test }

        Token[] tokens =
        {
            new Variable(),
            new Word(),
            new Assign(),
            new OpenBrace(),
            new NumberLiteral(),
            new Separator(),
            new NumberLiteral(),
            new Separator(),
            new Word(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var statements = parser.Parse().Values;

        Assert.Single(statements);
        Ronin.Grammar.Datum datum = statements[0];
        InlineList list = datum?.Initializer;
        Assert.NotNull(list);
    }
}
