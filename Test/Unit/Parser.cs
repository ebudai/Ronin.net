using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class parser
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
            new Number(),
            new Terminal(),

            // a = 6;

            new Word(),
            new Assign(),
            new Number(),
            new Terminal(),

            // 3..test;

            new Number(),
            new Ronin.Lexicon.Symbols.Range(),
            new Word(),
            new Terminal(),

            // function x (var a => number) y (cash on hand => money) { return cash on hand * a; }

            new Function(),
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

            new Datatype(),
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

            new Number(),
            new Terminal(),

            // (a, b, "text");

            new OpenParenthesis(),
            new Word(),
            new Separator(),
            new Word(),
            new Separator(),
            new Text(),
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
        var statements = parser.Parse();

        Assert.Equal(9, statements?.Count);

        Ronin.Grammar.ImportExportSyntax partof = statements[0];
        Assert.NotNull(partof);

        Ronin.Grammar.DatumDeclarationSyntax datum = statements[1];
        Assert.NotNull(datum);

        Ronin.Grammar.AssignmentSyntax assignment = statements[2];
        Assert.NotNull(assignment);

        Ronin.Grammar.IntervalSyntax interval = statements[3];
        Assert.NotNull(interval);

        Ronin.Grammar.FunctionDeclarationSyntax function = statements[4];
        Assert.NotNull(function);

        Ronin.Grammar.DatatypeDeclarationSyntax datatype = statements[5];
        Assert.NotNull(datatype);

        Ronin.Grammar.Value scalar_value = statements[6];
        Ronin.Grammar.LiteralSyntax scalar = scalar_value;
        Assert.NotNull(scalar);

        Ronin.Grammar.Value arguments_value = statements[7];
        Ronin.Grammar.Aggregates.Arguments arguments = arguments_value;
        Assert.NotNull(arguments);

        Ronin.Grammar.Aggregates.Scope scope = statements[8];
        Assert.NotNull(scope);
    }
}
