using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;
using Ronin.Lexicon.Literals;

namespace Failure;

public class TextLiteral
{
    [Fact(DisplayName = "without quotes")]
    public void Fail()
    {
        const string literal = "testtest";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "unterminated")]
    public void Unterminated()
    {
        const string literal = "\"testtest";

        Lexer lexer = new(literal);
        var lexed = lexer.Lex();

        Assert.Equal(3, lexed.Length);
        
        Assert.IsType<TextDelimiter>(lexed[0]);
        var quote = lexed[0] as TextDelimiter;
        Assert.Equal(new[] { '"' }, quote.Sourcecode.ToArray());

        Assert.IsType<Ronin.Lexicon.Word>(lexed[1]);
        var name = lexed[1] as Ronin.Lexicon.Word;
        Assert.Equal("testtest", name.ToString());
    }

    [Fact(DisplayName = "lone double quote")]
    public void DoubleQuote()
    {
        const string literal = "\"";

        Lexer lexer = new(literal);
        var lexed = lexer.Lex();

        Assert.NotEmpty(lexed);
        Assert.IsType<TextDelimiter>(lexed[0]);
        var quote = lexed[0] as TextDelimiter;
        Assert.Equal(literal.ToArray(), quote.Sourcecode.ToArray());
    }

    [Fact(DisplayName = "tricky unterminated")]
    public void TrickyUnterminated()
    {
        const string literal = "\"this is text\\\" unterminated";

        Lexer lexer = new(literal);
        var lexed = lexer.Lex();

        Assert.NotEmpty(lexed);
        foreach (var lexeme in lexed) Assert.IsNotType<Text>(lexeme);
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Literal.Lex(lexer);

        Assert.Null(lexed);
    }
}