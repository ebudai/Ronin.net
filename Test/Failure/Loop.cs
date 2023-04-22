using Ronin;
using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keyword;
using Ronin.Lexicon.Punctuation;

namespace Failure;

[Trait("Parser", null)]
public class Loop
{
    [Fact(DisplayName = $"doesn't start with {ForEach.keyword}")]
    public void NotALoop()
    {
        // not loop;

        Token[] tokens =
        {
            new Word(),
            new Word(),
            new Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var loop = Ronin.Grammar.Loop.Parse(ref parser);

        Assert.Null(loop);
    }

    [Fact(DisplayName = "bad name")]
    public void BadName()
    {
        // for each 7 in best horses { run the horse; }

        Token[] tokens =
        {
            new ForEach(),
            new NumberLiteral(),
            new Word(),
            new Word(),
            new OpenBrace(),
            new Word(),
            new Word(),
            new Word(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var loop = Ronin.Grammar.Loop.Parse(ref parser);

        Assert.Null(loop);
    }

    [Fact(DisplayName = "missing scope")]
    public void MissingScope()
    {
        // for each car in fast cars car colour = 3;

        Token[] tokens =
        {
            new ForEach(),
            new Word(),
            new Word(),
            new Word(),
            new Word(),
            new Word(),
            new Assign(),
            new NumberLiteral(),
            new Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var loop = Ronin.Grammar.Loop.Parse(ref parser);

        Assert.Null(loop);
    }
}
