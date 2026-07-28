using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait(nameof(Lexer), null)]
public class Words
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string name = "thing";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name, word?.Memory.ToString());
    }

    [Fact(DisplayName = "tokens compare by text")]
    public void TokensCompareByText()
    {
        // A token is a segment of the source, so two of them are the same token
        // when they spell the same thing — not when they are the same object. Name
        // equality is built on this, and so is every hash of a name.
        Lexer first = new("thing");
        Lexer second = new("thing");
        Lexer third = new("other");

        var thing = Word.Lex(ref first);
        var same = Word.Lex(ref second);
        var different = Word.Lex(ref third);

        Assert.True(thing.Equals(same));
        Assert.False(thing.Equals(different));
        Assert.False(thing.Equals("thing"));

        Assert.Equal(thing.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(thing.GetHashCode(), different.GetHashCode());
    }

    [Fact(DisplayName = "with terminator")]
    public void WithTerminator()
    {
        const string name = "thing;";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.Memory.ToString());
    }

    [Fact(DisplayName = "with separator")]
    public void WithSeparator()
    {
        const string name = "thing,";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.Memory.ToString());
    }

    [Fact(DisplayName = "with opening parenthesis")]
    public void WithOpeningParenthesis()
    {
        const string name = "thing(";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.Memory.ToString());
    }

    [Fact(DisplayName = "with closing parenthesis")]
    public void WithClosingParenthesis()
    {
        const string name = "thing)";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.Memory.ToString());
    }

    [Fact(DisplayName = "with opening bracket")]
    public void WithOpeningBracket()
    {
        const string name = "thing[";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.Memory.ToString());
    }

    [Fact(DisplayName = "with closing bracket")]
    public void WithClosingBracket()
    {
        const string name = "thing]";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.Memory.ToString());
    }

    [Fact(DisplayName = "with opening brace")]
    public void WithOpeningBrace()
    {
        const string name = "thing{";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.Memory.ToString());
    }

    [Fact(DisplayName = "with closing brace")]
    public void WithClosingBrace()
    {
        const string name = "thing}";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.Memory.ToString());
    }

    [Fact(DisplayName = "with single quote")]
    public void WithSingleQuote()
    {
        const string name = "thing'";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.Memory.ToString());
    }

    [Fact(DisplayName = "with double quote")]
    public void WithDoubleQuote()
    {
        const string name = "thing\"";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.Memory.ToString());
    }

    [Fact(DisplayName = "with space")]
    public void WithSpace()
    {
        const string name = "thing ";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.Memory.ToString());
    }
}
