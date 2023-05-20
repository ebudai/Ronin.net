using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keyword;
using Ronin.Lexicon.Punctuation;

namespace Unit;

[Trait("Parser", null)]
public class Parsing
{
    [Fact(DisplayName = "parse")]
    public void Parse()
    {
        Token[] tokens =
        {
            // part of testing apparatus;

            new PartOf(),
            new Word(),
            new Word(),
            new Terminal(),

            // var a = 3;

            new Variable(),
            new Word(),
            new Assign(),
            new NumberLiteral(),
            new Terminal(),

            // a = 6;

            new Word(),
            new Assign(),
            new NumberLiteral(),
            new Terminal(),

            // 3..test;

            new NumberLiteral(),
            new Ronin.Lexicon.Punctuation.Range(),
            new Word(),
            new Terminal(),

            // function x (var a => number) y (cash on hand => money) { return cash on hand * a; }

            new Ronin.Lexicon.Keyword.Function(),
            new Word(),
            new OpenParenthesis(),
            new Variable(),
            new Word(),
            new Returns(),
            new Word(),
            new CloseParenthesis(),
            new Word(),
            new OpenParenthesis(),
            new Word(),
            new Word(),
            new Word(),
            new Returns(),
            new Word(),
            new CloseParenthesis(),
            new OpenBrace(),
            new Word(),
            new Word(),
            new Word(),
            new Word(),
            new Asterisk(),
            new Word(),
            new Terminal(),
            new CloseBrace(),

            // datatype big thing { constant size => whole number; }

            new Ronin.Lexicon.Keyword.Datatype(),
            new Word(),
            new Word(),
            new OpenBrace(),
            new Constant(),
            new Word(),
            new Returns(),
            new Word(),
            new Word(),
            new Terminal(),
            new CloseBrace(),

            // 7;

            new NumberLiteral(),
            new Terminal(),

            // (a, b, "text");

            new OpenParenthesis(),
            new Word(),
            new Separator(),
            new Word(),
            new Separator(),
            new TextLiteral(),
            new CloseParenthesis(),
            new Terminal(),

            // { var x => moment; florb x now; }

            new OpenBrace(),
            new Variable(),
            new Word(),
            new Returns(),
            new Word(),
            new Terminal(),
            new Word(),
            new Word(),
            new Word(),
            new Terminal(),
            new CloseBrace(),

            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var statements = parser.Parse().Values;

        Assert.Equal(9, statements?.Count);

        var partof = statements[0] as Ronin.Grammar.Export;
        Assert.NotNull(partof);

        var datum = statements[1] as Ronin.Grammar.Datum;
        Assert.NotNull(datum);

        var assignment = statements[2] as Ronin.Grammar.Assignment;
        Assert.NotNull(assignment);

        var reference = statements[3] as Ronin.Grammar.Reference;
        Assert.NotNull(reference);

        var function = statements[4] as Ronin.Grammar.Function;
        Assert.NotNull(function);

        var datatype = statements[5] as Ronin.Grammar.Datatype;
        Assert.NotNull(datatype);

        var scalar = statements[6] as Ronin.Grammar.Literal;
        Assert.NotNull(scalar);

        var arguments = statements[7] as Ronin.Grammar.Compound.Inputs;
        Assert.NotNull(arguments);

        var scope = statements[8] as Ronin.Grammar.Compound.Scope;
        Assert.NotNull(scope);
    }
}
