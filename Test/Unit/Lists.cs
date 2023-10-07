using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;
using Literal = Ronin.Grammar.Literal;

namespace Unit;

[Trait("Parser", null)]
public class Lists : ParsingTests
{
    [Fact(DisplayName = "single")]
    public void Single()
    {
        // { 3 }

        List<Token> tokens = new()
        {
            StartScope(),
            Number(3),
            EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var list = List.Parse(ref parser);

        Assert.Single(list);
        var scalar = list[0] as Literal;
        Assert.Single(scalar?.Tokens.ToArray());
    }

    [Fact(DisplayName = "multiple")]
    public void Multiple()
    {
        // { 3, 4, 5 }

        List<Token> tokens = new()
        {
            StartScope(),
            Number(3),
            Separator(),
            Number(4),
            Separator(),
            Number(5),
            EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens.AsLinkedList());
        var list = List.Parse(ref parser);

        Assert.Equal(3, list?.Count);

        {
            var scalar = list[0] as Literal;
            Assert.Single(scalar?.Tokens.ToArray());
        }

        {
            var scalar = list[1] as Literal;
            Assert.Single(scalar?.Tokens.ToArray());
        }

        {
            var scalar = list[2] as Literal;
            Assert.Single(scalar?.Tokens.ToArray());
        }
    }

    [Fact(DisplayName = "as value")]
    public void AsValue()
    {
        // var x = { 5, 2, test }

        List<Token> tokens = new()
        {
            Keyword.Variable(),
            Word("x"),
            Assign(),
            StartScope(),
            Number(5),
            Separator(),
            Number(2),
            Separator(),
            Word("test"),
            EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens.AsLinkedList());
        var statements = parser.Parse().ToList();

        Assert.Single(statements);
        var datum = statements[0] as Datum;
        var list = datum?.Initializer as List;
        Assert.NotNull(list);
    }
}
