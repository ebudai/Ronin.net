using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class list
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // []
        Tokens tokens = new();
        tokens.Add<OpenSquareBracket>()
            .Add<CloseSquareBracket>();

        Parser parser = new(tokens.ToArray());
        var ordinal = Ordinal.Parse(ref parser);

        Assert.Empty(ordinal?.Values);
    }
}
