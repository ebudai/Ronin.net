using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class scope
{
    [Fact(DisplayName = "missing name")]
    public void MissingName()
    {
        // { ",;,thing }

        Token[] tokens =
        {
            new OpenBrace(),
            new DoubleQuote(),
            new Separator(),
            new Terminal(),
            new Separator(),
            new Word(),
            new CloseBrace(),
            new Terminal()
        };
        
        Parser parser = new(tokens);
        var scope = Scope.Parse(ref parser);

        Assert.Null(scope);
    }
}
