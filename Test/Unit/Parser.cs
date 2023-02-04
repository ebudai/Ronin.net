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

        // var a = 3;

        tokens.Add<Variable>()
            .Add<Word>("a")
            .Add<Assign>()
            .Add<Number>("3")
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

        Parser parser = new(tokens.ToArray());
        var statements = parser.Parse();

        Assert.Equal(3, statements?.Count);

        Ronin.Grammar.Datum datum = statements[0];
        Assert.NotNull(datum);

        Ronin.Grammar.Function function = statements[1];
        Assert.NotNull(function);

        Ronin.Grammar.Datatype datatype = statements[2];
        Assert.NotNull(datatype);
    }
}
