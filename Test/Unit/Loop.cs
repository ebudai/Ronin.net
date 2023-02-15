using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class loop
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string car = "car";
        const string cars = "cars";
        const string speed = "speed";
        const string nine_k = "9000";

        Tokens tokens = new();
        tokens.Add<ForEach>()
            .Add<Word>(car)
            .Add<In>()
            .Add<Word>(cars)
            .Add<OpenBrace>()
            .Add<Word>(car)
            .Add<Word>(speed)
            .Add<Assign>()
            .Add<Number>(nine_k)
            .Add<Terminal>()
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        var loop = Loop.Parse(ref parser);

        Assert.False(loop?.Mutable);

        Assert.Single(loop.Variable?.Words);
        Assert.Equal(car, loop.Variable.Words[0]);

        Reference reference = loop.List;
        Assert.Single(reference?.Components);
        Name name = reference.Components[0];
        Assert.Single(name?.Words);
        Assert.Equal(cars, name.Words[0]);

        Assert.Single(loop.Body?.Values);
        Assignment assignment = loop.Body.Values[0];
        Assert.NotNull(assignment);
    }
}
