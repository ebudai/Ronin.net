using Ronin.Compiler;
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
    public void NotAnArguments()
    {
        // not an object;

        Tokens tokens = new();
        tokens.Add<Word>("not")
            .Add<Word>("an")
            .Add<Word>("object")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        var arguments = Arguments.Parse(ref parser);

        Assert.Null(arguments);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        Tokens tokens = new();

        Parser parser = new(tokens.ToArray());
        var arguments = Arguments.Parse(ref parser);

        Assert.Null(arguments);
    }

    [Fact(DisplayName = "bad separator")]
    public void BadSeparator()
    {
        // (test, (thing;stuff))

        Tokens tokens = new();
        tokens.Add<OpenParenthesis>()
            .Add<Word>("test")
            .Add<Separator>()
            .Add<OpenParenthesis>()
            .Add<Word>("thing")
            .Add<Terminal>()
            .Add<Word>("stuff")
            .Add<CloseParenthesis>()
            .Add<CloseParenthesis>();

        Parser parser = new(tokens.ToArray());
        var arguments = Arguments.Parse(ref parser);
        Assert.Null(arguments);
    }

    [Fact(DisplayName = "terminated incorrectly")]
    public void TerminatedWrong()
    {
        // (test;)

        Tokens tokens = new();
        tokens.Add<OpenParenthesis>()
            .Add<Word>("test")
            .Add<Terminal>()
            .Add<CloseParenthesis>();

        Parser parser = new(tokens.ToArray());
        var arguments = Arguments.Parse(ref parser);
        Assert.Null(arguments);
    }
}
