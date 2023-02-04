using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;
using Test;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class ordinal
{
    [Fact(DisplayName = "does not start with [")]
    public void NotAnOrdinal()
    {
        // not an ordinal;

        Tokens tokens = new();
        tokens.Add<Word>("not")
            .Add<Word>("an")
            .Add<Word>("ordinal")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var ordinal = Ordinal.Parse(ref parser);

        Assert.Null(ordinal);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        Tokens tokens = new();

        Parser parser = new(tokens.ToArray());
        var arguments = Ordinal.Parse(ref parser);

        Assert.Null(arguments);
    }

    [Fact(DisplayName = "bad component")]
    public void BadComponent()
    {
        // [test, (thing;stuff)]

        Tokens tokens = new();
        tokens.Add<OpenSquareBracket>()
            .Add<Word>("test")
            .Add<Separator>()
            .Add<OpenParenthesis>()
            .Add<Word>("thing")
            .Add<Terminal>()
            .Add<Word>("stuff")
            .Add<CloseParenthesis>()
            .Add<CloseSquareBracket>();

        Parser parser = new(tokens.ToArray());
        var ordinal = Ordinal.Parse(ref parser);

        Assert.Null(ordinal);
    }

    [Fact(DisplayName = "terminated incorrectly")]
    public void TerminatedWrong()
    {
        // [test;]

        Tokens tokens = new();
        tokens.Add<OpenSquareBracket>()
            .Add<Word>("test")
            .Add<Terminal>()
            .Add<CloseSquareBracket>();

        Parser parser = new(tokens.ToArray());
        var ordinal = Ordinal.Parse(ref parser);

        Assert.Null(ordinal);
    }
}
