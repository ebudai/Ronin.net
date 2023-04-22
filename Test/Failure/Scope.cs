using Ronin;
using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Punctuation;

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
            new OpenBrace(),
            new TextDelimiter(),
            new Separator(),
            new Terminal(),
            new Separator(),
            new Word(),
            new CloseBrace()
        };
        
        Parser parser = new(tokens);
        var scope = Ronin.Grammar.Compound.Scope.Parse(ref parser);

        Assert.Null(scope);
    }
}
