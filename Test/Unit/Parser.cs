using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

using Datatype = Ronin.Grammar.Datatype;
using Function = Ronin.Grammar.Function;

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

            Keyword.PartOf(),
            Word("testing"),
            Word("apparatus"),
            Terminal(),

            // var a = 3;

            Keyword.Variable(),
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

            Keyword.Function(),
            Word("x"),
            StartValues(),
            Keyword.Variable(),
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

            Keyword.Datatype(),
            Word("big"),
            Word("thing"),
            StartScope(),
            Keyword.Constant(),
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
            Keyword.Variable(),
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

        var partof = statements[0] as Export;
        Assert.NotNull(partof);

        var datum = statements[1] as Datum.Declaration;
        Assert.NotNull(datum);

        var assignment = statements[2] as Assignment;
        Assert.NotNull(assignment);

        var functioncall = statements[3] as Function.Call;
        Assert.NotNull(functioncall);

        var function = statements[4] as Function.Declaration;
        Assert.NotNull(function);

        var datatype = statements[5] as Datatype.Declaration;
        Assert.NotNull(datatype);

        var scalar = statements[6] as Inline;
        Assert.NotNull(scalar);

        var arguments = statements[7] as Inputs;
        Assert.NotNull(arguments);

        var scope = statements[8] as Scope;
        Assert.NotNull(scope);
    }
}
