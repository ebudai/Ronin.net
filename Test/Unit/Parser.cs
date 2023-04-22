using Ronin;
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

        Ronin.Grammar.ImportExport partof = statements[0];
        Assert.NotNull(partof);

        Ronin.Grammar.Datum datum = statements[1];
        Assert.NotNull(datum);

        Ronin.Grammar.Assignment assignment = statements[2];
        Assert.NotNull(assignment);

        Ronin.Grammar.Interval interval = statements[3];
        Assert.NotNull(interval);

        Ronin.Grammar.Function function = statements[4];
        Assert.NotNull(function);

        Ronin.Grammar.Datatype datatype = statements[5];
        Assert.NotNull(datatype);

        Ronin.Grammar.Value scalar_value = statements[6];
        Ronin.Grammar.Literal scalar = scalar_value;
        Assert.NotNull(scalar);

        Ronin.Grammar.Value arguments_value = statements[7];
        Ronin.Grammar.Compound.Arguments arguments = arguments_value;
        Assert.NotNull(arguments);

        Ronin.Grammar.Compound.Scope scope = statements[8];
        Assert.NotNull(scope);
    }
}
