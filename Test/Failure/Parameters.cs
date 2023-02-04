using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;
using Test;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class parameters
{
    [Fact(DisplayName = "does not start with (")]
    public void NotParameters()
    {
        // not parameters;

        Tokens tokens = new();
        tokens.Add<Word>("not")
            .Add<Word>("parameters")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var parameters = Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        Tokens tokens = new();

        Parser parser = new(tokens.ToArray());
        var parameters = Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }

    [Fact(DisplayName = "bad component")]
    public void BadComponent()
    {
        // (test => money, [thing;stuff])

        Tokens tokens = new();
        tokens.Add<OpenParenthesis>()
            .Add<Word>("test")
            .Add<Returns>()
            .Add<Word>("money")
            .Add<Separator>()
            .Add<OpenSquareBracket>()
            .Add<Word>("thing")
            .Add<Terminal>()
            .Add<Word>("stuff")
            .Add<CloseSquareBracket>()
            .Add<CloseParenthesis>();

        Parser parser = new(tokens.ToArray());
        var parameters = Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }

    [Fact(DisplayName = "terminated incorrectly")]
    public void TerminatedWrong()
    {
        // (test => text;)

        Tokens tokens = new();
        tokens.Add<OpenParenthesis>()
            .Add<Word>("test")
            .Add<Returns>()
            .Add<Word>("text")
            .Add<Terminal>()
            .Add<CloseParenthesis>();

        Parser parser = new(tokens.ToArray());
        var parameters = Parameters.Parse(ref parser);

        Assert.Null(parameters);
    }
}
