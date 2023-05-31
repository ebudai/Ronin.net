using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

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
            new StartScope(),
            new Number(),
            new EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var list = Ronin.Grammar.Compound.List.Parse(ref parser);

        Assert.Single(list?.Values);
        var scalar = list.Values[0] as Ronin.Grammar.Literal;
        Assert.Equal(1, scalar?.Source.Length);
    }

    [Fact(DisplayName = "multiple")]
    public void Multiple()
    {
        // { 3, 4, 5 }

        Token[] tokens =
        {
            new StartScope(),
            new Number(),
            new Separator(),
            new Number(),
            new Separator(),
            new Number(),
            new EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var list = Ronin.Grammar.Compound.List.Parse(ref parser);

        Assert.Equal(3, list?.Values?.Count);

        {
            var scalar = list.Values[0] as Ronin.Grammar.Literal;
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            var scalar = list.Values[1] as Ronin.Grammar.Literal;
            Assert.Equal(1, scalar?.Source.Length);
        }

        {
            var scalar = list.Values[2] as Ronin.Grammar.Literal;
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
            new StartScope(),
            new Number(),
            new Separator(),
            new Number(),
            new Separator(),
            new Word(),
            new EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var statements = parser.Parse().Values;

        Assert.Single(statements);
        var datum = statements[0] as Ronin.Grammar.DatumDeclaration;
        var list = datum?.Initializer as Ronin.Grammar.Compound.List;
        Assert.NotNull(list);
    }
}
