using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class parser
{
    [Fact(DisplayName = "parse")]
    public void Parse()
    {
        Tokens tokens = new();

        // part of testing apparatus;

        tokens.Add<PartOf>()
            .Add<Word>("testing")
            .Add<Word>("apparatus")
            .Add<Terminal>();

        // var a = 3;

        tokens.Add<Variable>()
            .Add<Word>("a")
            .Add<Assign>()
            .Add<Number>("3")
            .Add<Terminal>();

        // a = 6;

        tokens.Add<Word>("a")
            .Add<Assign>()
            .Add<Number>("6")
            .Add<Terminal>();

        // function x (var a => number) y (cash on hand => money) { return cash on hand * a; }

        tokens.Add<Function>()
            .Add<Word>("x")
            .Add<OpenParenthesis>()
            .Add<Variable>()
            .Add<Word>("a")
            .Add<Returns>()
            .Add<Word>("number")
            .Add<CloseParenthesis>()
            .Add<Word>("y")
            .Add<OpenParenthesis>()
            .Add<Word>("cash")
            .Add<Word>("on")
            .Add<Word>("hand")
            .Add<Returns>()
            .Add<Word>("money")
            .Add<CloseParenthesis>()
            .Add<OpenBrace>()
            .Add<Word>("return")
            .Add<Word>("cash")
            .Add<Word>("on")
            .Add<Word>("hand")
            .Add<Asterisk>()
            .Add<Word>("a")
            .Add<Terminal>()
            .Add<CloseBrace>();

        // datatype big thing { constant size => whole number; }

        tokens.Add<Datatype>()
            .Add<Word>("big")
            .Add<Word>("thing")
            .Add<OpenBrace>()
            .Add<Constant>()
            .Add<Word>("size")
            .Add<Returns>()
            .Add<Word>("whole")
            .Add<Word>("number")
            .Add<Terminal>()
            .Add<CloseBrace>();

        // 7;

        tokens.Add<Number>("7")
            .Add<Terminal>();

        // (a, b, "text");

        tokens.Add<OpenParenthesis>()
            .Add<Word>("a")
            .Add<Separator>()
            .Add<Word>("b")
            .Add<Separator>()
            .Add<Text>("\"text\"")
            .Add<CloseParenthesis>()
            .Add<Terminal>();

        // { var x => moment; florb x now; }

        tokens.Add<OpenBrace>()
            .Add<Variable>()
            .Add<Word>("x")
            .Add<Returns>()
            .Add<Word>("moment")
            .Add<Terminal>()
            .Add<Word>("florb")
            .Add<Word>("x")
            .Add<Word>("now")
            .Add<Terminal>()
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        var statements = parser.Parse();

        Assert.Equal(8, statements?.Count);

        Ronin.Grammar.Hierarchy partof = statements[0];
        Assert.NotNull(partof);

        Ronin.Grammar.Datum datum = statements[1];
        Assert.NotNull(datum);

        Ronin.Grammar.Assignment assignment = statements[2];
        Assert.NotNull(assignment);

        Ronin.Grammar.Function function = statements[3];
        Assert.NotNull(function);

        Ronin.Grammar.Datatype datatype = statements[4];
        Assert.NotNull(datatype);

        Ronin.Grammar.Value scalar_value = statements[5];
        Ronin.Grammar.Scalar scalar = scalar_value;
        Assert.NotNull(scalar);

        Ronin.Grammar.Value arguments_value = statements[6];
        Ronin.Grammar.Aggregates.Arguments arguments = arguments_value;
        Assert.NotNull(arguments);

        Ronin.Grammar.Aggregates.Scope scope = statements[7];
        Assert.NotNull(scope);
    }
}
