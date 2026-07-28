using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using System.Collections;
using Test;

namespace Unit;

[Trait(nameof(Parser), null)]
public class Identifiers : ParsingTests
{
    [Fact(DisplayName = "symbols end an identifier")]
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

        // This asserted a single three-token name, back when Name.Parse merged
        // symbols into the name beside them. An identifier declares something, and
        // the operators are a fixed table the language owns, so a declaration
        // cannot introduce one: the identifier is «name» and it ends at the «+».
        Assert.Single(identifier);
        Assert.Single(identifier[0].AsName.Tokens.ToArray());
        Assert.Equal(name, identifier[0].AsName.Tokens.Span[0].Memory.ToString());
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

        // four words, not seven: the whitespace between them bounds the name and
        // is not part of it. This asserted seven while AdvanceTo sized its array
        // by the running index, which counts the trivia it then skips.
        Assert.Equal(4, identifier[0].AsName.Tokens.Length);
        Assert.Equal("name all the things", identifier[0].AsName.Words);
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

        Assert.Equal(first.Identifier[0].AsName.Tokens.Span[0].Memory, second.Identifier[0].AsName.Tokens.Span[0].Memory);
    }

    /// <summary>A string with the same characters and a different identity.</summary>
    private static string Rebuilt(string text) => new(text.ToCharArray());

    [Fact(DisplayName = "names compare by their words")]
    public void NamesCompareByTheirWords()
    {
        // Two occurrences of a name in different statements are different token
        // objects spelling the same thing, and resolution has to see them as one.
        // Built from distinct string instances on purpose. This asserted hash
        // equality and passed by accident while the hash compared the backing
        // memory object rather than its characters — interning arranged for two
        // names written as the same literal to share one instance.
        Name first = new() { Tokens = new[] { Word("cash"), Word("on"), Word("hand") } };
        Name same = new() { Tokens = new[] { Word(Rebuilt("cash")), Word(Rebuilt("on")), Word(Rebuilt("hand")) } };
        Name shorter = new() { Tokens = new[] { Word("cash"), Word("on") } };
        Name different = new() { Tokens = new[] { Word("cash"), Word("in"), Word("hand") } };

        Assert.True(first.Equals(same));
        Assert.False(first.Equals(shorter));
        Assert.False(first.Equals(different));
        Assert.False(first.Equals("cash on hand"));

        Assert.Equal(first.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(first.GetHashCode(), different.GetHashCode());
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
