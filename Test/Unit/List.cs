using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;

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
            new OpenBraceSymbol(),
            new NumberLiteral(),
            new CloseBraceSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var list = InlineListSyntax.Parse(ref parser);

        Assert.Single(list?.Values);
        LiteralSyntax scalar = list.Values[0];
        Assert.Single(scalar?.Source);
    }

    [Fact(DisplayName = "multiple")]
    public void Multiple()
    {
        // { 3, 4, 5 }

        Token[] tokens =
        {
            new OpenBraceSymbol(),
            new NumberLiteral(),
            new SeparatorSymbol(),
            new NumberLiteral(),
            new SeparatorSymbol(),
            new NumberLiteral(),
            new CloseBraceSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var list = InlineListSyntax.Parse(ref parser);

        Assert.Equal(3, list?.Values?.Count);

        {
            LiteralSyntax scalar = list.Values[0];
            Assert.Single(scalar?.Source);
        }

        {
            LiteralSyntax scalar = list.Values[1];
            Assert.Single(scalar?.Source);
        }

        {
            LiteralSyntax scalar = list.Values[2];
            Assert.Single(scalar?.Source);
        }
    }

    [Fact(DisplayName = "as value")]
    public void AsValue()
    {
        // var x = { 5, 2, test }

        Token[] tokens =
        {
            new VariableKeyword(),
            new Word(),
            new AssignSymbol(),
            new OpenBraceSymbol(),
            new NumberLiteral(),
            new SeparatorSymbol(),
            new NumberLiteral(),
            new SeparatorSymbol(),
            new Word(),
            new CloseBraceSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var statements = parser.Parse();

        Assert.Single(statements);
        DatumDeclarationSyntax datum = statements[0];
        InlineListSyntax list = datum?.Initializer;
        Assert.NotNull(list);
    }
}
