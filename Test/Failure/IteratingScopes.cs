using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait(nameof(Parser), null)]
public class IteratingScopes : ParsingTests
{
    [Fact(DisplayName = $"doesn't start with {ForEach.keyword}")]
    public void NotALoop()
    {
        // not loop;

        List<Token> tokens = new()
        {
            Word("not"),
            Word("loop"),
            Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var loop = Scope.Parse(ref parser);

        Assert.Null(loop);
    }

    [Fact(DisplayName = "bad name")]
    public void BadName()
    {
        // for each 7 in best horses { run the horse; }

        List<Token> tokens = new()
        {
            Keyword.ForEach(),
            Number(7),
            Word("in"),
            Word("best"),
            Word("horses"),
            StartScope(),
            Word("run"),
            Word("the"),
            Word("horse"),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var loop = Scope.Parse(ref parser);

        Assert.Null(loop);
    }

    [Fact(DisplayName = "missing scope")]
    public void MissingScope()
    {
        // for each car in fast cars car colour = 3;

        List<Token> tokens = new()
        {
            Keyword.ForEach(),
            Word("car"),
            Word("in"),
            Word("fast"),
            Word("cars"),
            Word("car"),
            Word("colour"),
            Assign(),
            Number(3),
            Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var loop = Scope.Parse(ref parser);

        Assert.Null(loop);
    }
}
