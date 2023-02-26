using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Failure;

[Trait("Parser", null)]
public class Loop
{
    [Fact(DisplayName = $"doesn't start with {ForEachKeyword.keyword}")]
    public void NotALoop()
    {
        // not loop;

        Token[] tokens =
        {
            new Word(),
            new Word(),
            new TerminalSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var loop = LoopSyntax.Parse(ref parser);

        Assert.Null(loop);
    }

    [Fact(DisplayName = "bad name")]
    public void BadName()
    {
        // for each 7 in best horses { run the horse; }

        Token[] tokens =
        {
            new ForEachKeyword(),
            new NumberLiteral(),
            new Word(),
            new Word(),
            new OpenBraceSymbol(),
            new Word(),
            new Word(),
            new Word(),
            new TerminalSymbol(),
            new CloseBraceSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var loop = LoopSyntax.Parse(ref parser);

        Assert.Null(loop);
    }

    [Fact(DisplayName = "missing scope")]
    public void MissingScope()
    {
        // for each car in fast cars car colour = 3;

        Token[] tokens =
        {
            new ForEachKeyword(),
            new Word(),
            new Word(),
            new Word(),
            new Word(),
            new Word(),
            new AssignSymbol(),
            new NumberLiteral(),
            new TerminalSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var loop = LoopSyntax.Parse(ref parser);

        Assert.Null(loop);
    }
}
