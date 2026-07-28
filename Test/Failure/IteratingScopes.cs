using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait(nameof(Parser), null)]
public class IteratingScopes : ParsingTests
{
    [Fact(DisplayName = $"doesn't start with {Iterate.keyword}")]
    public void NotALoop()
    {
        // not loop;

        List<Token> tokens = new()
        {
            Word("not"),
            Word("loop"),
            Terminal(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var loop = Scope.Parse(ref parser);

        Assert.Null(loop);
    }

    [Fact(DisplayName = "bad name")]
    public void BadName()
    {
        // iterate best horses => 7 { run the horse; }

        List<Token> tokens = new()
        {
            Keyword.Iterate(),
            Word("best"),
            Word("horses"),
            Returns(),
            Number(7),
            StartScope(),
            Word("run"),
            Word("the"),
            Word("horse"),
            Terminal(),
            EndScope(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var loop = Scope.Parse(ref parser);

        Assert.IsType<Scope.Iterating.ExpectedNameError>(loop);
    }

    [Fact(DisplayName = "missing returns")]
    public void MissingReturns()
    {
        // iterate cars car fast colour = 3;

        List<Token> tokens = new()
        {
            Keyword.Iterate(),
            Word("cars"),
            Word("car"),
            Word("fast"),
            Word("colour"),
            Assign(),
            Number(3),
            Terminal(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var loop = Scope.Parse(ref parser);

        Assert.IsType<Scope.Iterating.ExpectedReturnsSymbolError>(loop);
    }

    [Fact(DisplayName = "missing iterable")]
    public void MissingIterable()
    {
        // iterate => car fast colour = 3;

        List<Token> tokens = new()
        {
            Keyword.Iterate(),
            Returns(),
            Word("car"),
            Word("fast"),
            Word("colour"),
            Assign(),
            Number(3),
            Terminal(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var loop = Scope.Parse(ref parser);

        Assert.IsType<Scope.Iterating.ExpectedIterableError>(loop);
    }
}
