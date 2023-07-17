using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Parsing : ParsingTests
{
    [Fact(DisplayName = "parse")]
    public void Parse()
    {
        List<Token> tokens = new()
        {
            // part of testing apparatus;

            PartOf(),
            Word("testing"),
            Word("apparatus"),
            Terminal(),

            // var a = 3;

            Variable(),
            Word("a"),
            Assign(),
            Number(3),
            Terminal(),

            // a = 6;

            Word("a"),
            Assign(),
            Number(6),
            Terminal(),

            // 3..test;

            Number(3),
            Range(),
            Word("test"),
            Terminal(),

            // function x (var a => number) y (cash on hand => money) { return cash on hand * a; }

            Function(),
            Word("x"),
            StartValues(),
            Variable(),
            Word("a"),
            Returns(),
            Word("number"),
            EndValues(),
            Word("y"),
            StartValues(),
            Word("cash"),
            Word("on"),
            Word("hand"),
            Returns(),
            Word("money"),
            EndValues(),
            StartScope(),
            Word("return"),
            Word("cash"),
            Word("on"),
            Word("hand"),
            Symbol("*"),
            Word("a"),
            Terminal(),
            EndScope(),

            // datatype big thing { constant size => whole number; }

            Datatype(),
            Word("big"),
            Word("thing"),
            StartScope(),
            Constant(),
            Word("size"),
            Returns(),
            Word("whole"),
            Word("number"),
            Terminal(),
            EndScope(),

            // 7;

            Number(7),
            Terminal(),

            // (a, b, "text");

            StartValues(),
            Word("a"),
            Separator(),
            Word("b"),
            Separator(),
            Text("text"),
            EndValues(),
            Terminal(),

            // compiled { var x => moment; florb x now; }

            StartScope(),
            Variable(),
            Word("x"),
            Returns(),
            Word("moment"),
            Terminal(),
            Word("florb"),
            Word("x"),
            Word("now"),
            Terminal(),
            EndScope(),

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

        var scalar = statements[6] as Ronin.Grammar.Inline;
        Assert.NotNull(scalar);

        var arguments = statements[7] as Ronin.Grammar.Compound.Inputs;
        Assert.NotNull(arguments);

        var scope = statements[8] as Ronin.Grammar.Scope;
        Assert.NotNull(scope);
    }
}
