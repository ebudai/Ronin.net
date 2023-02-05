using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class scope
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        Tokens tokens = new();
        tokens.Add<OpenBrace>()
            .Add<Variable>()
            .Add<Word>("test")
            .Add<Assign>()
            .Add<Number>("56")
            .Add<Terminal>()
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        var scope = Scope.Parse(ref parser);

        Assert.Single(scope?.Values);

        Datum datum = scope.Values[0];

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Single(datum.Name?.Words);
        Assert.Equal("test", datum.Name.Words[0]);

        Assert.False(datum.Is.Optional);
        Assert.False(datum.Is.Persistent);
        Assert.False(datum.Is.Compiled);
        Assert.False(datum.Is.Shared);

        Scalar scalar = datum.Initializer;
        Assert.Single(scalar?.Literals);
        Assert.Equal("56", scalar.Literals[0]?.ToString());
    }
}
