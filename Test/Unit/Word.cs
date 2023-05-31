using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait("Lexer", null)]
public class Words
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string name = "thing";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name, word?.ToString());
    }

    [Fact(DisplayName = "with terminator")]
    public void WithTerminator()
    {
        const string name = "thing;";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.ToString());
    }

    [Fact(DisplayName = "with separator")]
    public void WithSeparator()
    {
        const string name = "thing,";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.ToString());
    }

    [Fact(DisplayName = "with opening parenthesis")]
    public void WithOpeningParenthesis()
    {
        const string name = "thing(";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.ToString());
    }

    [Fact(DisplayName = "with closing parenthesis")]
    public void WithClosingParenthesis()
    {
        const string name = "thing)";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.ToString());
    }

    [Fact(DisplayName = "with opening bracket")]
    public void WithOpeningBracket()
    {
        const string name = "thing[";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.ToString());
    }

    [Fact(DisplayName = "with closing bracket")]
    public void WithClosingBracket()
    {
        const string name = "thing]";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.ToString());
    }

    [Fact(DisplayName = "with opening brace")]
    public void WithOpeningBrace()
    {
        const string name = "thing{";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.ToString());
    }

    [Fact(DisplayName = "with closing brace")]
    public void WithClosingBrace()
    {
        const string name = "thing}";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.ToString());
    }

    [Fact(DisplayName = "with single quote")]
    public void WithSingleQuote()
    {
        const string name = "thing'";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.ToString());
    }

    [Fact(DisplayName = "with double quote")]
    public void WithDoubleQuote()
    {
        const string name = "thing\"";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.ToString());
    }

    [Fact(DisplayName = "with space")]
    public void WithSpace()
    {
        const string name = "thing ";

        Lexer lexer = new(name);
        var word = Word.Lex(ref lexer);

        Assert.Equal(name[..^1], word?.ToString());
    }
}
