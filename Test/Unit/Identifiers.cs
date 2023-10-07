using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait(nameof(Parser), null)]
public class Identifiers : ParsingTests
{
    [Fact(DisplayName = "symbols")]
    public void Symbols()
    {
        const string name = nameof(name);
        const string plus = "+";
        const string things = nameof(things);

        // name + things

        List<Token> tokens = new()
        {
            Word(name),
            Symbol(plus),
            Word(things),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var identifier = Identifier.Parse(ref parser);

        Assert.Equal(3, identifier?.Components.Count);
    }

    [Fact(DisplayName = "words")]
    public void Words()
    {
        const string name = nameof(name);
        const string all = nameof(all);
        const string the = nameof(the);
        const string things = nameof(things);

        // name all the things

        List<Token> tokens = new()
        {
            Word(name),
            Whitespace(),
            Word(all),
            Whitespace(),
            Word(the),
            Whitespace(),
            Word(things),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var identifier = Identifier.Parse(ref parser);

        Assert.Equal(4, identifier?.Components.Count);
    }

    [Fact(DisplayName = "equality")]
    public void Equality()
    {
        const string things = nameof(things);
        const string number = nameof(number);
        const string money = nameof(money);

        // var things => number;
        // var things => money;

        List<Token> tokens = new()
        {
            Keyword.Variable(),
            Word(things),
            Returns(),
            Word(number),
            Terminal(),
            Keyword.Variable(),
            Word(things),
            Returns(),
            Word(money),
            Terminal()
        };

        Parser parser = new(tokens);
        var first = Datum.Parse(ref parser);
        parser.Advance();
        var second = Datum.Parse(ref parser);

        Assert.Single(first.Identifier.Components);
        Assert.Single(second.Identifier.Components);

        Assert.Equal(first.Identifier.Components[0].GetHashCode(), second.Identifier.Components[0].GetHashCode());
        Assert.Equal(first.Identifier.Components[0], second.Identifier.Components[0]);
    }
}
