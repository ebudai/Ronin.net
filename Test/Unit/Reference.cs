using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class reference
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        Tokens tokens = new();
        tokens.Add<Word>("thing")
            .Add<Number>("7")
            .Add<OpenParenthesis>()
            .Add<Text>("\"stuff\"")
            .Add<CloseParenthesis>();

        Parser parser = new(tokens.ToArray());
        var reference = Reference.Parse(ref parser);

        Assert.Equal(3, reference?.Components?.Count);

        Name name = reference.Components[0];
        Assert.Equal("thing", string.Join(" ", name?.Words ?? new List<string>()));

        Scalar scalar = reference.Components[1];
        Assert.Single(scalar?.Literals);
        Assert.Equal("7", scalar.Literals[0]?.Sourcecode.ToString());

        Arguments arguments = reference.Components[2];
        Assert.Single(arguments?.Values);
        scalar = arguments.Values[0];
        Assert.Single(scalar?.Literals);
        Assert.Equal("\"stuff\"", scalar.Literals[0]?.Sourcecode.ToString());
    }
}
