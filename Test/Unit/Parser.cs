using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

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
            new StartValues(),
            new Variable(),
            new Word(),
            new Returns(),
            new Word(),
            new EndValues(),
            new Word(),
            new StartValues(),
            new Word(),
            new Word(),
            new Word(),
            new Returns(),
            new Word(),
            new EndValues(),
            new StartScope(),
            new Word(),
            new Word(),
            new Word(),
            new Word(),
            new Symbol { sourcecode = new[] { '*' } },
            new Word(),
            new Terminal(),
            new EndScope(),

            // datatype big thing { constant size => whole number; }

            new Datatype(),
            new Word(),
            new Word(),
            new StartScope(),
            new Constant(),
            new Word(),
            new Returns(),
            new Word(),
            new Word(),
            new Terminal(),
            new EndScope(),

            // 7;

            new Number(),
            new Terminal(),

            // (a, b, "text");

            new StartValues(),
            new Word(),
            new Separator(),
            new Word(),
            new Separator(),
            new Text(),
            new EndValues(),
            new Terminal(),

            // compiled { var x => moment; florb x now; }

            new StartScope(),
            new Variable(),
            new Word(),
            new Returns(),
            new Word(),
            new Terminal(),
            new Word(),
            new Word(),
            new Word(),
            new Terminal(),
            new EndScope(),

            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var statements = parser.Parse().Values;

        Assert.Equal(9, statements?.Count);

        var partof = statements[0] as Ronin.Grammar.Export;
        Assert.NotNull(partof);

        var datum = statements[1] as Ronin.Grammar.DatumDeclaration;
        Assert.NotNull(datum);

        var assignment = statements[2] as Ronin.Grammar.Assignment;
        Assert.NotNull(assignment);

        var reference = statements[3] as Ronin.Grammar.Reference;
        Assert.NotNull(reference);

        var function = statements[4] as Ronin.Grammar.FunctionDeclaration;
        Assert.NotNull(function);

        var datatype = statements[5] as Ronin.Grammar.DatatypeDeclaration;
        Assert.NotNull(datatype);

        var scalar = statements[6] as Ronin.Grammar.Literal;
        Assert.NotNull(scalar);

        var arguments = statements[7] as Ronin.Grammar.Compound.Inputs;
        Assert.NotNull(arguments);

        var scope = statements[8] as Ronin.Grammar.Scope;
        Assert.NotNull(scope);
    }
}
