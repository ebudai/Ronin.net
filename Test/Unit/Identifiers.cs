using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using System.Collections;
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
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var identifier = Identifier.Parse(ref parser);

        Assert.Single(identifier);
        Assert.Equal(3, identifier[0].AsT0.Tokens.Length);
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
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var identifier = Identifier.Parse(ref parser);

        Assert.Single(identifier);
        Assert.Equal(7, identifier[0].AsT0.Tokens.Length);
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

        Parser parser = new(tokens.AsLinkedList());
        var first = Datum.Parse(ref parser);
        parser.Advance();
        var second = Datum.Parse(ref parser);

        Assert.Single(first.Identifier);
        Assert.Single(second.Identifier);

        Assert.Equal(first.Identifier[0].AsT0.Tokens.Span[0].Memory, second.Identifier[0].AsT0.Tokens.Span[0].Memory);
    }

    [Fact(DisplayName = "enumerable")]
    public void Enumerable()
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
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var identifier = Identifier.Parse(ref parser);
        IEnumerable enumerable = identifier;

        Assert.Equivalent(enumerable.GetEnumerator(), identifier.GetEnumerator());
    }
}
