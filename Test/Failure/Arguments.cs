using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;
using Test;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class arguments
{
    [Fact(DisplayName = "does not start with (")]
    public void NotAnObject()
    {
        Tokens tokens = new();
        tokens.Add<Word>("not")
            .Add<Word>("an")
            .Add<Word>("object")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var aggregate = Arguments.Parse(ref parser);

        Assert.Null(aggregate);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        Tokens tokens = new();

        Parser parser = new(tokens.ToArray());
        var arguments = Arguments.Parse(ref parser);

        Assert.Null(arguments);
    }

    [Fact(DisplayName = "recursive bad syntax")]
    public void RecursiveBadSyntax()
    {
        Tokens tokens = new();
        tokens.Add<OpenParenthesis>()
            .Add<Word>("test")
            .Add<Separator>()
            .Add<OpenParenthesis>()
            .Add<Word>("thing")
            .Add<Terminal>()
            .Add<Word>("stuff")
            .Add<CloseParenthesis>()
            .Add<CloseParenthesis>()
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var syntax = Arguments.Parse(ref parser);
        Assert.IsType<Unknown>(syntax);
    }

    [Fact(DisplayName = "terminated incorrectly")]
    public void TerminatedWrong()
    {
        Tokens tokens = new();
        tokens.Add<OpenParenthesis>()
            .Add<Word>("test")
            .Add<Terminal>()
            .Add<CloseParenthesis>()
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var syntax = Arguments.Parse(ref parser);
        Assert.IsType<Unknown>(syntax);
    }
}
