using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class lookup
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string billy = "\"Billy\"";
        const string seven = "7";

        Tokens tokens = new();
        tokens.Add<OpenBrace>()
            .Add<Text>(billy)
            .Add<Assign>()
            .Add<Number>(seven)
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        var lookup = Lookup.Parse(ref parser);

        Assert.Single(lookup?.Values);
        var association = lookup.Values[0];
        
        Scalar key = association.Key;
        Assert.Single(key?.Literals);
        Assert.Equal(billy, key.Literals[0]?.ToString());

        Scalar value = association.Value;
        Assert.Single(value?.Literals);
        Assert.Equal(seven, value.Literals[0]?.ToString());
    }
}
