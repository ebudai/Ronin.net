using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

[Trait("Parser", null)]
public class Scope
{
    [Fact(DisplayName = "missing name")]
    public void MissingName()
    {
        // { ",;,thing }

        Token[] tokens =
        {
            new OpenBraceSymbol(),
            new TextDelimiterSymbol(),
            new SeparatorSymbol(),
            new TerminalSymbol(),
            new SeparatorSymbol(),
            new Word(),
            new CloseBraceSymbol(),
            new TerminalSymbol()
        };
        
        Parser parser = new(tokens);
        var scope = Ronin.Grammar.Aggregates.Scope.Parse(ref parser);

        Assert.Null(scope);
    }
}
