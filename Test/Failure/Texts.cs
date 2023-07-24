using Ronin.Compiler;
using Ronin.Lexicon;

namespace Failure;

[Trait("Lexer", null)]
public class Texts
{
    [Fact(DisplayName = "without quotes")]
    public void Fail()
    {
        const string literal = "testtest";

        Lexer lexer = new(literal);
        var lexed = Literal.Lex(ref lexer);

        Assert.IsNotType<Literal>(lexed);
    }

    [Fact(DisplayName = "unterminated")]
    public void Unterminated()
    {
        const string literal = "\"testtest";

        Lexer lexer = new(literal);
        var tokens = lexer.Lex().ToArray();

        Assert.Equal(3, tokens.Length);

        Assert.IsType<TextDelimiter>(tokens[0]);

        var word = tokens[1] as Word;
        Assert.Equal(literal[1..], word?.Memory.ToArray());
    }

    [Fact(DisplayName = "lone double quote")]
    public void DoubleQuote()
    {
        const string literal = "\"";

        Lexer lexer = new(literal);
        var lexed = lexer.Lex().ToArray();

        Assert.Equal(2, lexed.Length);

        var quote = lexed[0] as TextDelimiter;
        Assert.Equal(literal, quote?.Memory.ToArray());
    }

    [Fact(DisplayName = "tricky unterminated")]
    public void TrickyUnterminated()
    {
        const string literal = "\"this is text\\\" unterminated";

        Lexer lexer = new(literal);
        var lexed = lexer.Lex();

        foreach (var lexeme in lexed.ToArray()) Assert.IsNotType<Text>(lexeme);
    }

    [Fact(DisplayName = "no data")]
    public void NoData()
    {
        Lexer lexer = new(string.Empty);
        var lexed = Literal.Lex(ref lexer);

        Assert.Null(lexed);
    }
}